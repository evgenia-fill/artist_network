async function request(path, options) {
  // Моки используют префикс /api/editor. При подключении бэкенда передавайте сюда полный путь контроллера.
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
  // Если проекты приходят вместе со списком друзей, собрать их здесь в projects и указать ownerId
  // для каждого проекта. Для отображения лайков нужны likeCount и likedByMe для текущего пользователя.
  // Файл и история уже доступны через GET /api/images/{id}/file и GET /api/images/{id}/versions.
  // Сопоставить ImageVersionDto с моделью UI: Id для запросов, Number для подписи версии.
  // Для версий, отрисованных сервером, не накладывать фильтры повторно.
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

export function setProjectLike(projectId, liked) {
  // TODO(API): заменить мок-маршрут здесь, когда появится контроллер лайков.
  // Мок ставит лайк через PUT и снимает через DELETE. Сопоставить методы с контрактом бэкенда.
  // Редактор ожидает обновлённый проект. Если API вернёт только likeCount и likedByMe,
  // объединить эти поля с текущим проектом при обновлении состояния в Editor.updateProject.
  return request(`/projects/${encodeURIComponent(projectId)}/like`, {
    method: liked ? 'PUT' : 'DELETE',
  });
}
