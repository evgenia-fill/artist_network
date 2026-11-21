import { adjustments, changedOperations } from './operations.js';

const tabs = [
  ['adjustments', 'Правки'],
  ['versions', 'Версии'],
  ['file', 'Файл'],
  ['friends', 'Друзья'],
];

const dateTime = new Intl.DateTimeFormat('ru-RU', {
  day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit',
});
const date = new Intl.DateTimeFormat('ru-RU', { timeZone: 'UTC' });

export function Identity({ user, description = user.description }) {
  return (
    <>
      <span className="av" aria-hidden="true">{user.name[0]}</span>
      <span>
        <span className="user-name">{user.name}</span>
        <span className="user-description">{description}</span>
      </span>
    </>
  );
}

function Adjustments({ operations, onAdjust, busy }) {
  return adjustments.map(({ key, label, min, max, step, unit }) => (
    <div className="sl" key={key}>
      <div className="slider-label">
        <label htmlFor={key}>{label}</label>
        <span aria-hidden="true">{operations[key]}{unit}</span>
      </div>
      <input
        id={key}
        type="range"
        min={min}
        max={max}
        step={step}
        value={operations[key]}
        aria-valuetext={`${operations[key]}${unit}`}
        onChange={(event) => onAdjust(key, event.target.valueAsNumber)}
        disabled={busy}
      />
    </div>
  ));
}

function Versions({ project, activeVersionId, onSelect, busy }) {
  if (!project.versions.length) return <p>Сохранённых версий пока нет</p>;

  return project.versions.toReversed().map((version) => {
    const active = activeVersionId === version.id;
    const operations = changedOperations(version.operations);

    return (
      <button
        key={version.id}
        className={active ? 'ver on' : 'ver'}
        aria-pressed={active}
        onClick={() => onSelect(version.id)}
        disabled={busy}
      >
        <b>v{version.number}</b>{' '}
        <time dateTime={version.created_at}>{dateTime.format(new Date(version.created_at))}</time>
        <code>{Object.keys(operations).length ? JSON.stringify(operations) : 'без изменений'}</code>
        <code>result_file_id: {version.result_file_id}</code>
      </button>
    );
  });
}

function FileDetails({ image, storage }) {
  return (
    <dl className="kv">
      <dt>image.id</dt><dd>{image.id}</dd>
      <dt>size</dt><dd>{image.size} байт</dd>
      <dt>width × height</dt><dd>{image.width} × {image.height}</dd>
      <dt>project_id</dt><dd>{image.project_id}</dd>
      <dt>storage.id</dt><dd>{storage.id}</dd>
      <dt>storage.link</dt><dd>{storage.link}</dd>
      <dt>создано</dt><dd>{dateTime.format(new Date(storage.created_at))}</dd>
    </dl>
  );
}

function Friends({ friends, activeFriendId, onSelect, busy }) {
  if (!friends.length) return <p>Друзей пока нет</p>;

  return friends.map((friend) => (
    <button
      className="fr"
      key={friend.id}
      aria-pressed={activeFriendId === friend.id}
      disabled={busy}
      onClick={() => onSelect(friend)}
    >
      <Identity
        user={friend}
        description={`${friend.description} · с ${date.format(new Date(friend.friendsSince))}`}
      />
    </button>
  ));
}

export function EditorSidebar({
  project, activeVersionId, friends, activeFriendId, onFriendSelect, operations, onAdjust, onVersionSelect,
  tab, onTabChange, busy, error,
}) {
  return (
    <aside className="right" aria-label="Параметры редактора">
      <div className="tabs" role="group" aria-label="Разделы редактора">
        {tabs.filter(([id]) => !activeFriendId || id !== 'adjustments').map(([id, label]) => (
          <button
            key={id}
            className={id === tab ? 'on' : undefined}
            aria-pressed={id === tab}
            onClick={() => onTabChange(id)}
          >
            {label}
          </button>
        ))}
      </div>
      {error && <p className="error" role="alert">{error}</p>}
      {tab === 'friends' && (
        <Friends friends={friends} activeFriendId={activeFriendId} onSelect={onFriendSelect} busy={busy} />
      )}
      {!project && tab !== 'friends' && <p>Сначала выберите проект</p>}
      {project && tab === 'adjustments' && <Adjustments operations={operations} onAdjust={onAdjust} busy={busy} />}
      {project && tab === 'versions' && (
        <Versions project={project} activeVersionId={activeVersionId} onSelect={onVersionSelect} busy={busy} />
      )}
      {project && tab === 'file' && <FileDetails image={project.image} storage={project.storage} />}
    </aside>
  );
}
