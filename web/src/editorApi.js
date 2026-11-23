import { changedOperations } from './operations.js';
import { getToken, getAuthUser } from './auth.js';

export const mockMode = import.meta.env.MODE === 'mock';
export const maxUploadBytes = 10 * 1024 * 1024;

const imagesPath = '/api/images';
const storageKey = 'pixelflow.imageIds';
const user = getAuthUser();

if (!user) {
  throw new Error('Необходимо войти в аккаунт');
}

async function request(path, options = {}) {
  const headers = new Headers(options.headers || {});
  const token = getToken();

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(path, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    const error = new Error('Сессия истекла. Войдите снова.');
    error.status = 401;
    throw error;
  }

  if (!response.ok) {
    const error = new Error(`Ошибка API: ${response.status}`);
    error.status = response.status;
    throw error;
  }

  if (response.status === 204) return null;

  return response.json();
}

function jsonPost(body) {
  return {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  };
}

function readImageIds() {
  try {
    const ids = JSON.parse(localStorage.getItem(storageKey));
    return Array.isArray(ids) ? ids.filter((id) => typeof id === 'string') : [];
  } catch {
    return [];
  }
}

function rememberImage(id) {
  try {
    localStorage.setItem(storageKey, JSON.stringify([...new Set([...readImageIds(), id])]));
  } catch {
    // The uploaded image remains available for this session if storage is disabled.
  }
}

function toVersion({ id, number, operation, createdAt }) {
  return { id, number, operation, operations: {}, created_at: createdAt };
}

function toProject(id, versions, current) {
  return {
    id,
    ownerId: user.id,
    currentVersionId: current.id,
    versions,
    image: {
      id,
      image_link: `${imagesPath}/${encodeURIComponent(id)}/file?v=${current.id}`,
      size: current.size,
      width: current.width,
      height: current.height,
    },
  };
}

function withVersion(project, current) {
  const versions = project.versions.some((version) => version.id === current.id)
    ? project.versions
    : [...project.versions, toVersion(current)];
  return toProject(project.id, versions, current);
}

export async function loadEditor(signal) {
  if (mockMode) return request('/api/editor', { signal });

  const projects = await Promise.all(readImageIds().map(async (id) => {
    try {
      const versions = await request(`${imagesPath}/${encodeURIComponent(id)}/versions`, { signal });
      const current = versions.find((version) => version.isCurrent);
      return current ? toProject(id, versions.map(toVersion), current) : null;
    } catch (error) {
      if (error.status === 404) return null;
      throw error;
    }
  }));
  return { user, projects: projects.filter(Boolean), friends: [] };
}

export async function uploadImage(file) {
  if (file.size < 1 || file.size > maxUploadBytes) {
    throw new Error('Размер файла должен быть от 1 байта до 10 МиБ (10 485 760 байт)');
  }

  const body = new FormData();
  body.append('file', file);
  const { imageId, version } = await request(imagesPath, { method: 'POST', body });
  rememberImage(imageId);
  return toProject(imageId, [toVersion(version)], version);
}

export async function saveVersion(project, operations) {
  if (mockMode) {
    return request(`/api/editor/projects/${encodeURIComponent(project.id)}/versions`, jsonPost({
      operations: changedOperations(operations),
    }));
  }

  const steps = [];
  const degrees = ((operations.rotate % 360) + 360) % 360;
  // CSS rotate(...) scaleX(...) applies the reflection before the rotation.
  if (operations.flipX) steps.push({ type: 'flip', axis: 'horizontal' });
  if (degrees) steps.push({ type: 'rotate', degrees });
  if (!steps.length) return project;

  const [{ version }] = await request(`${imagesPath}/batch/apply-template`, jsonPost({
    imageIds: [project.id],
    steps,
  }));
  return withVersion(project, version);
}

export async function restoreVersion(project, versionId) {
  const prefix = mockMode ? '/api/editor/projects' : imagesPath;
  const result = await request(
    `${prefix}/${encodeURIComponent(project.id)}/versions/${encodeURIComponent(versionId)}/restore`,
    { method: 'POST' },
  );
  return mockMode ? result : withVersion(project, result);
}

export async function resetImage(project) {
  const version = await request(`${imagesPath}/${encodeURIComponent(project.id)}/reset`, { method: 'POST' });
  return withVersion(project, version);
}

export function setProjectLike(projectId, liked) {
  return request(`/api/editor/projects/${encodeURIComponent(projectId)}/like`, {
    method: liked ? 'PUT' : 'DELETE',
  });
}
