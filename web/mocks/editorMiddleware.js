import { fixtures } from './fixtures.js';
import { adjustments, changedOperations } from '../src/operations.js';

const prefix = '/api/editor';
const versionsRoute = /^\/projects\/([^/]+)\/versions(?:\/([^/]+)\/restore)?$/;

function sendJson(response, status, data) {
  response.writeHead(status, {
    'Content-Type': 'application/json; charset=utf-8',
    'Cache-Control': 'no-store',
  });
  response.end(JSON.stringify(data));
}

function validOperations(operations) {
  if (!operations || typeof operations !== 'object' || Array.isArray(operations)) return false;

  return Object.entries(operations).every(([key, value]) => {
    if (key === 'rotate') return Number.isSafeInteger(value) && value % 90 === 0;
    if (key === 'flipX') return typeof value === 'boolean';
    const field = adjustments.find((item) => item.key === key);
    return field && Number.isFinite(value) && value >= field.min && value <= field.max
      && (value - field.min) % field.step === 0;
  });
}

async function readJson(request) {
  let body = '';
  request.setEncoding('utf8');
  for await (const chunk of request.iterator({ destroyOnReturn: false })) {
    body += chunk;
    if (body.length > 4096) {
      request.resume();
      throw new Error('Слишком большое тело запроса');
    }
  }
  return JSON.parse(body);
}

function editorState() {
  const db = structuredClone(fixtures);
  const [user] = db.users;
  const projects = db.projects.map((project) => {
    const image = db.images.find((item) => item.id === project.source_id);
    return {
      id: project.id,
      currentVersionId: project.version_id,
      image,
      storage: db.storages.find((item) => item.id === image.storage_id),
      versions: db.versions.filter((version) => version.project_id === project.id),
    };
  });

  return {
    user,
    projects,
    usedBytes: db.images.reduce((total, image) => total + image.size, 0),
    quotaBytes: 50_000_000,
    friends: db.friendships.filter((friendship) => friendship.user_id === user.id).map((friendship) => ({
      ...db.users.find((item) => item.id === friendship.friend_id),
      friendsSince: friendship.created_at,
    })),
  };
}

export function mockEditor() {
  function configure(server) {
    const state = editorState();
    let nextVersionId = Math.max(...fixtures.versions.map((version) => version.id)) + 1;

    server.middlewares.use(async (request, response, next) => {
      const path = request.url.split('?')[0];
      if (path !== prefix && !path.startsWith(`${prefix}/`)) return next();

      const route = path.slice(prefix.length);
      if (!route && request.method === 'GET') return sendJson(response, 200, state);

      const match = versionsRoute.exec(route);
      const project = match && state.projects.find((item) => String(item.id) === match[1]);
      if (!project) return sendJson(response, 404, { message: 'Маршрут или проект не найден' });
      if (request.method !== 'POST') {
        response.setHeader('Allow', 'POST');
        return sendJson(response, 405, { message: 'Используйте POST' });
      }

      if (match[2]) {
        const version = project.versions.find((item) => String(item.id) === match[2]);
        if (!version) return sendJson(response, 404, { message: 'Версия не найдена' });
        project.currentVersionId = version.id;
        return sendJson(response, 200, project);
      }

      let operations;
      try {
        ({ operations } = await readJson(request));
      } catch {
        return sendJson(response, 400, { message: 'Ожидалось корректное тело JSON' });
      }
      if (!validOperations(operations)) {
        return sendJson(response, 400, { message: 'Недопустимые параметры правок' });
      }

      const id = nextVersionId++;
      const number = Math.max(0, ...project.versions.map((version) => version.number)) + 1;
      project.versions.push({
        id,
        number,
        operations: changedOperations(operations),
        project_id: project.id,
        result_file_id: `f-${project.id}${String(id).padStart(2, '0')}`,
        created_at: new Date().toISOString(),
      });
      project.currentVersionId = id;
      return sendJson(response, 201, project);
    });
  }

  return { name: 'pixelflow-mock-editor', configureServer: configure, configurePreviewServer: configure };
}
