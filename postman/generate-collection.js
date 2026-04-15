#!/usr/bin/env node
/**
 * generate-collection.js
 * Reads openapi.json from the project root and generates a Postman v3 YAML
 * collection under postman/collections/kanban-api/ and an environment file
 * at postman/environments/local.env.yaml.
 *
 * Usage: node postman/generate-collection.js
 */

'use strict';

const fs   = require('fs');
const path = require('path');

// ---------------------------------------------------------------------------
// Paths
// ---------------------------------------------------------------------------
const PROJECT_ROOT      = path.resolve(__dirname, '..');
const OPENAPI_PATH      = path.join(PROJECT_ROOT, 'openapi.json');
const COLLECTION_DIR    = path.join(PROJECT_ROOT, 'postman', 'collections', 'kanban-api');
const ENVIRONMENTS_DIR  = path.join(PROJECT_ROOT, 'postman', 'environments');
const ENV_FILE          = path.join(ENVIRONMENTS_DIR, 'local.env.yaml');

// ---------------------------------------------------------------------------
// Endpoints that do NOT require auth (AllowAnonymous)
// ---------------------------------------------------------------------------
const ANONYMOUS_PATHS = new Set([
  '/api/auth/register',
  '/api/auth/login',
]);

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Ensure a directory exists (recursive). */
function mkdirp(dir) {
  fs.mkdirSync(dir, { recursive: true });
}

/**
 * Sanitise a string so it can be used as a filesystem filename stem.
 * Replaces characters that are illegal on common OSes with a hyphen.
 */
function sanitiseFilename(str) {
  return str.replace(/[/\\:*?"<>|]/g, '-').replace(/-+/g, '-').replace(/^-|-$/g, '');
}

/**
 * Convert an operationId or path+method into a human-readable name.
 * e.g. "PostApiAuthRegister" → "Post Api Auth Register"
 *      "/api/auth/register" + "post" → "Post /api/auth/register"
 */
function humaniseName(operationId, method, urlPath) {
  if (operationId) {
    // Split on camelCase / PascalCase boundaries
    return operationId.replace(/([a-z])([A-Z])/g, '$1 $2').trim();
  }
  const m = method.charAt(0).toUpperCase() + method.slice(1).toLowerCase();
  return `${m} ${urlPath}`;
}

/**
 * Derive a sensible placeholder value for a JSON schema property.
 * Handles $ref by looking it up in the components/schemas map.
 */
function placeholderValue(propName, propSchema, schemas) {
  if (!propSchema) return null;

  // Resolve $ref
  if (propSchema.$ref) {
    const refName = propSchema.$ref.replace('#/components/schemas/', '');
    propSchema = (schemas && schemas[refName]) || {};
  }

  const type   = propSchema.type;
  const format = propSchema.format || '';
  const name   = (propName || '').toLowerCase();

  // Enum → first value
  if (propSchema.enum && propSchema.enum.length > 0) return propSchema.enum[0];

  // Nullable / allOf with single entry
  if (propSchema.allOf && propSchema.allOf.length === 1) {
    return placeholderValue(propName, propSchema.allOf[0], schemas);
  }

  if (propSchema.nullable) {
    // Still try to give a typed value
  }

  switch (type) {
    case 'integer':
    case 'number':
      if (name.includes('id'))   return 1;
      if (name.includes('role')) return 0;
      return 0;

    case 'boolean':
      return false;

    case 'array':
      return [];

    case 'object':
      return {};

    case 'string':
    default:
      if (format === 'date-time') return '2024-01-01T00:00:00Z';
      if (format === 'date')      return '2024-01-01';
      if (format === 'uuid')      return '00000000-0000-0000-0000-000000000000';
      if (format === 'email' || name.includes('email')) return 'user@example.com';
      if (name.includes('password'))                    return 'Password123!';
      if (name.includes('firstname') || name === 'firstname') return 'John';
      if (name.includes('lastname')  || name === 'lastname')  return 'Doe';
      if (name.includes('title'))   return 'Example Title';
      if (name.includes('description') || name.includes('desc')) return 'Example description';
      if (name.includes('name'))    return 'Example Name';
      if (name.includes('url'))     return 'https://example.com';
      if (name.includes('token'))   return 'example_token';
      if (name.includes('color') || name.includes('colour')) return '#FFFFFF';
      if (propSchema.nullable)      return null;
      return 'example_value';
  }
}

/**
 * Build a JSON body object from an OpenAPI schema object.
 * Returns a plain JS object (will be JSON.stringify'd later).
 */
function buildBodyFromSchema(schema, schemas) {
  if (!schema) return null;

  // Resolve $ref at top level
  if (schema.$ref) {
    const refName = schema.$ref.replace('#/components/schemas/', '');
    schema = (schemas && schemas[refName]) || {};
  }

  if (schema.type === 'object' || schema.properties) {
    const obj = {};
    const props = schema.properties || {};
    for (const [key, propSchema] of Object.entries(props)) {
      obj[key] = placeholderValue(key, propSchema, schemas);
    }
    return obj;
  }

  if (schema.allOf) {
    // Merge all allOf entries
    const merged = {};
    for (const sub of schema.allOf) {
      const resolved = sub.$ref
        ? (schemas && schemas[sub.$ref.replace('#/components/schemas/', '')]) || {}
        : sub;
      Object.assign(merged, buildBodyFromSchema(resolved, schemas) || {});
    }
    return merged;
  }

  return null;
}

/**
 * Indent every line of a multi-line string by `spaces` spaces.
 */
function indent(str, spaces) {
  const pad = ' '.repeat(spaces);
  return str.split('\n').map(l => pad + l).join('\n');
}

/**
 * YAML-quote a scalar string value.
 * Uses single quotes when the value contains special characters.
 * Returns the value as-is for simple strings.
 */
function yamlScalar(value) {
  if (typeof value !== 'string') return String(value);
  // Characters that require quoting in YAML
  const needsQuote = /[:{}\[\],#&*!|>'"%@`]/.test(value) || value.includes('{{') || value === '';
  if (needsQuote) {
    // Escape any single quotes inside the value
    return `'${value.replace(/'/g, "''")}'`;
  }
  return value;
}

/**
 * Render a single request YAML file content string.
 */
function renderRequestYaml({ name, method, url, requiresAuth, bodyObject }) {
  const lines = [];

  lines.push(`$kind: http-request`);
  lines.push(`name: ${yamlScalar(name)}`);
  lines.push(`method: ${method.toUpperCase()}`);
  lines.push(`url: ${yamlScalar(url)}`);

  // Headers
  const needsBody = ['POST', 'PUT', 'PATCH'].includes(method.toUpperCase()) && bodyObject !== null;
  const hasHeaders = needsBody || requiresAuth;

  if (hasHeaders) {
    lines.push(`headers:`);
    if (needsBody) {
      lines.push(`  - key: Content-Type`);
      lines.push(`    value: application/json`);
    }
    if (requiresAuth) {
      lines.push(`  - key: Authorization`);
      lines.push(`    value: 'Bearer {{token}}'`);
    }
  }

  // Body
  if (needsBody && bodyObject !== null) {
    const jsonStr = JSON.stringify(bodyObject, null, 2);
    lines.push(`body:`);
    lines.push(`  type: json`);
    lines.push(`  content: |-`);
    // Indent the JSON block by 4 spaces
    for (const l of jsonStr.split('\n')) {
      lines.push(`    ${l}`);
    }
  }

  return lines.join('\n') + '\n';
}

/**
 * Render the collection definition YAML.
 */
function renderCollectionDefinition(collectionName) {
  return [
    `$kind: collection`,
    `name: ${yamlScalar(collectionName)}`,
    `variables:`,
    `  - key: baseUrl`,
    `    value: 'http://localhost:5081'`,
  ].join('\n') + '\n';
}

/**
 * Render the environment YAML.
 */
function renderEnvironmentYaml() {
  return [
    `name: Local`,
    `values:`,
    `  - key: baseUrl`,
    `    value: 'http://localhost:5081'`,
    `    enabled: true`,
    `    type: default`,
    `  - key: token`,
    `    value: ''`,
    `    enabled: true`,
    `    type: default`,
  ].join('\n') + '\n';
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

function main() {
  // 1. Read openapi.json
  if (!fs.existsSync(OPENAPI_PATH)) {
    console.error('Error: openapi.json not found. Run `make openapi-spec` first.');
    process.exit(1);
  }

  let spec;
  try {
    spec = JSON.parse(fs.readFileSync(OPENAPI_PATH, 'utf8'));
  } catch (err) {
    console.error(`Error: Failed to parse openapi.json — ${err.message}`);
    process.exit(1);
  }

  const schemas = (spec.components && spec.components.schemas) || {};
  const paths   = spec.paths || {};

  // 2. Group operations by tag
  // tagGroups: Map<tagName, Array<{method, urlPath, operation}>>
  const tagGroups = new Map();

  for (const [urlPath, pathItem] of Object.entries(paths)) {
    const HTTP_METHODS = ['get', 'post', 'put', 'patch', 'delete', 'head', 'options'];
    for (const method of HTTP_METHODS) {
      const operation = pathItem[method];
      if (!operation) continue;

      const tags = (operation.tags && operation.tags.length > 0)
        ? operation.tags
        : ['Default'];

      for (const tag of tags) {
        if (!tagGroups.has(tag)) tagGroups.set(tag, []);
        tagGroups.get(tag).push({ method, urlPath, operation });
      }
    }
  }

  // 3. Write collection definition
  mkdirp(COLLECTION_DIR);
  const defDir = path.join(COLLECTION_DIR, '.resources');
  mkdirp(defDir);
  fs.writeFileSync(
    path.join(defDir, 'definition.yaml'),
    renderCollectionDefinition('Kanban API'),
    'utf8'
  );

  // 4. Write request files
  let totalRequests = 0;
  let totalFolders  = 0;

  for (const [tag, operations] of tagGroups) {
    totalFolders++;
    const folderDir = path.join(COLLECTION_DIR, tag);
    mkdirp(folderDir);

    // Track used filenames within this folder to avoid collisions
    const usedFilenames = new Set();

    let orderCounter = 1000;

    for (const { method, urlPath, operation } of operations) {
      // Determine name
      const name = humaniseName(operation.operationId, method, urlPath);

      // Determine URL
      const url = `{{baseUrl}}${urlPath}`;

      // Determine auth requirement
      const requiresAuth = !ANONYMOUS_PATHS.has(urlPath);

      // Determine body
      let bodyObject = null;
      if (['post', 'put', 'patch'].includes(method.toLowerCase())) {
        const requestBody = operation.requestBody;
        if (requestBody) {
          const content = requestBody.content || {};
          const jsonContent = content['application/json'];
          if (jsonContent && jsonContent.schema) {
            bodyObject = buildBodyFromSchema(jsonContent.schema, schemas);
          }
        }
      }

      // Build YAML content
      const yaml = renderRequestYaml({ name, method, url, requiresAuth, bodyObject });

      // Determine filename
      let filenameStem = sanitiseFilename(
        operation.operationId
          ? operation.operationId.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase()
          : `${method.toLowerCase()}-${urlPath.replace(/\//g, '-').replace(/^-/, '')}`
      );

      // Ensure uniqueness
      let filename = `${filenameStem}.request.yaml`;
      let suffix = 1;
      while (usedFilenames.has(filename.toLowerCase())) {
        filename = `${filenameStem}-${suffix++}.request.yaml`;
      }
      usedFilenames.add(filename.toLowerCase());

      fs.writeFileSync(path.join(folderDir, filename), yaml, 'utf8');
      totalRequests++;
      orderCounter += 1000;
    }

    console.log(`  [${tag}] ${operations.length} request(s) → postman/collections/kanban-api/${tag}/`);
  }

  // 5. Write environment file
  mkdirp(ENVIRONMENTS_DIR);
  fs.writeFileSync(ENV_FILE, renderEnvironmentYaml(), 'utf8');
  console.log(`  [env] postman/environments/local.env.yaml`);

  // 6. Summary
  console.log('');
  console.log(`Done. Created ${totalRequests} request(s) in ${totalFolders} folder(s).`);
  console.log(`Environment saved to postman/environments/local.env.yaml`);
  console.log('');
  console.log('Next steps:');
  console.log('  1. Run `make openapi-spec` if you haven\'t already.');
  console.log('  2. Run `node postman/generate-collection.js` to (re)generate the collection.');
  console.log('  3. Open Postman and select the "Local" environment.');
  console.log('  4. Send the Login request and copy the token into the {{token}} variable.');
}

main();
