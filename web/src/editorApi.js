async function request(path, options) {
  const response = await fetch(`/api/editor${path}`, options);
  if (!response.ok) {
    const error = await response.json().catch(() => null);
    throw new Error(error?.message ?? `Запрос завершился с ошибкой (${response.status})`);
  }

  if (!response.headers.get('content-type')?.includes('application/json')) {
    throw new Error('Ожидался JSON от API редактора. Проверьте конфигурацию сервера.');
  }

  return response.json();
}

export function loadEditor(signal) {
  // TODO(API): подключить профиль, квоту хранилища, проекты и друзей, когда появятся их маршруты.
  // Файл и история уже доступны через GET /api/images/{id}/file и GET /api/images/{id}/versions.
  // Сопоставить ImageVersionDto с моделью UI. Для версий, отрисованных сервером, не накладывать фильтры повторно.
  // Здесь же задать учётные данные, когда будет определён контракт авторизации.
  return request('', { signal });
}

export function saveVersion(projectId, operations) {
  // TODO(API): подключить атомарное сохранение снимка правок.
  // API изображений поддерживает rotate, flip, reset и batch/apply-template с шагами rotate/flip.
  // Для сохранения всех фильтров одним запросом нужен согласованный серверный контракт.
  return request(`/projects/${encodeURIComponent(projectId)}/versions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ operations }),
  });
}

export function restoreVersion(projectId, versionId) {
  // TODO(API): использовать POST /api/images/{imageId}/versions/{versionId}/restore.
  // Передавать GUID изображения и версии. После ответа обновить текущую версию и URL файла.
  return request(`/projects/${encodeURIComponent(projectId)}/versions/${encodeURIComponent(versionId)}/restore`, {
    method: 'POST',
  });
}
