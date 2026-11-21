import { useEffect, useState } from 'react';
import { EditorSidebar, Identity } from './EditorSidebar.jsx';
import { loadEditor, restoreVersion, saveVersion, setProjectLike } from './editorApi.js';
import { changedOperations, defaults, imageFilter } from './operations.js';

function megabytes(bytes) {
  return `${(bytes / 1_000_000).toFixed(1)} МБ`;
}

function currentVersion(project) {
  return project.versions.find((version) => version.id === project.currentVersionId);
}

function currentOperations(project) {
  return { ...defaults, ...currentVersion(project)?.operations };
}

function Header({ user, usedBytes, quotaBytes, onReturn, busy }) {
  function toggleTheme() {
    const { dataset } = document.documentElement;
    const systemDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const dark = dataset.theme ? dataset.theme === 'dark' : systemDark;
    dataset.theme = dark ? 'light' : 'dark';
  }

  return (
    <header>
      <div className="logo"><i aria-hidden="true" />Лаборатория</div>
      <div className="sp" />
      <div className="store">
        <div>{user.storage_link}</div>
        <div>{megabytes(usedBytes)} из {megabytes(quotaBytes)}</div>
        <progress className="bar" value={usedBytes} max={quotaBytes} aria-label="Заполненность хранилища" />
      </div>
      <div className="me"><Identity user={user} /></div>
      {onReturn && (
        <button className="btn" disabled={busy} onClick={onReturn}>Вернуться к своим проектам</button>
      )}
      <button className="btn" onClick={toggleTheme} aria-label="Сменить тему">Тема</button>
    </header>
  );
}

function ProjectList({ owner, projects, projectId, onSelect, busy }) {
  const title = owner ? `Проекты · ${owner.name}` : 'Мои проекты';

  return (
    <aside aria-label={title}>
      <h3>{title}</h3>
      {projects.length === 0 && <p>Проектов пока нет</p>}
      {projects.map(({ id, image, versions, likeCount, likedByMe }) => (
        <button
          key={id}
          className={id === projectId ? 'proj on' : 'proj'}
          aria-pressed={id === projectId}
          disabled={busy}
          onClick={() => onSelect(id)}
        >
          <img src={image.image_link} alt="" />
          <span>
            Проект #{id}
            <small>{versions.length} верс. · {megabytes(image.size)}</small>
            <small className={likedByMe ? 'liked' : undefined}>
              <span aria-hidden="true">{likedByMe ? '♥' : '♡'} </span>Лайки: {likeCount}
            </small>
          </span>
        </button>
      ))}
    </aside>
  );
}

function Workspace({ project, owner, operations, setOperations, onSave, onReset, onLike, busy }) {
  if (!project) {
    return <main className="page-status">{owner ? 'У друга пока нет проектов' : 'Выберите проект, чтобы открыть редактор'}</main>;
  }

  const { image } = project;
  const version = currentVersion(project);
  const { rotate, flipX } = operations;
  const quarterTurn = rotate % 180 !== 0;
  const style = {
    '--image-width': `${image.width}px`,
    '--fit-width': quarterTurn ? '100cqh' : '100cqw',
    '--fit-height': quarterTurn ? '100cqw' : '100cqh',
    '--image-ratio': image.width / image.height,
    filter: imageFilter(operations),
    transform: `rotate(${rotate}deg) scaleX(${flipX ? -1 : 1})`,
  };

  const turn = (degrees) => setOperations((value) => ({ ...value, rotate: value.rotate + degrees }));
  const flip = () => setOperations((value) => ({ ...value, flipX: !value.flipX }));

  return (
    <main>
      <div className="tools">
        {owner ? (
          <div className="project-owner">
            <b>Проект #{project.id}</b>
            <span className="user-description">{owner.name}</span>
          </div>
        ) : (
          <>
            <button className="btn" disabled={busy} onClick={() => turn(-90)}>Повернуть ⟲</button>
            <button className="btn" disabled={busy} onClick={() => turn(90)}>Повернуть ⟳</button>
            <button className="btn" disabled={busy} onClick={flip} aria-pressed={flipX}>Отразить</button>
            <button className="btn" disabled={busy} onClick={onReset}>Сбросить</button>
          </>
        )}
        <span className="sp" />
        <button className="btn like" disabled={busy} onClick={onLike} aria-pressed={project.likedByMe}>
          <span aria-hidden="true">{project.likedByMe ? '♥' : '♡'}</span> Лайк <span>{project.likeCount}</span>
        </button>
        {!owner && <button className="btn pri" disabled={busy} onClick={onSave}>Сохранить версию</button>}
      </div>
      <div className="stage">
        <img src={image.image_link} alt={owner ? `Изображение проекта #${project.id}` : 'Редактируемое изображение'} style={style} />
      </div>
      <div className="meta">
        <span>{image.width}×{image.height}</span>
        <span>{megabytes(image.size)}</span>
        {version && <span>Версия v{version.number}</span>}
      </div>
    </main>
  );
}

function Editor({ initialData }) {
  const firstProject = initialData.projects.find((item) => item.ownerId === initialData.user.id);
  const [data, setData] = useState(initialData);
  const [ownProjectId, setOwnProjectId] = useState(firstProject?.id);
  const [operations, setOperations] = useState(() => firstProject ? currentOperations(firstProject) : defaults);
  const [friendView, setFriendView] = useState(null);
  const [tab, setTab] = useState('adjustments');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(null);
  const owner = friendView?.friend;
  const projects = data.projects.filter((item) => item.ownerId === (owner?.id ?? data.user.id));
  const projectId = friendView ? friendView.projectId : ownProjectId;
  const project = projects.find((item) => item.id === projectId);

  useEffect(() => {
    if (!notice) return;
    const timeout = window.setTimeout(() => setNotice(null), 1800);
    return () => window.clearTimeout(timeout);
  }, [notice]);

  function openProject(id) {
    if (friendView) {
      setFriendView({ ...friendView, projectId: id });
    } else {
      setOwnProjectId(id);
      setOperations(currentOperations(projects.find((item) => item.id === id)));
    }
    setError('');
  }

  function openFriend(friend) {
    if (owner?.id === friend.id) return;
    const firstFriendProject = data.projects.find((item) => item.ownerId === friend.id);
    setFriendView({ friend, projectId: firstFriendProject?.id });
    setError('');
  }

  function returnToOwnProjects() {
    setFriendView(null);
    setError('');
  }

  function reset() {
    setOperations(defaults);
    setNotice({ text: 'Правки сброшены' });
  }

  async function updateProject(action, onUpdated) {
    setBusy(true);
    setError('');
    try {
      const updated = await action();
      setData((previous) => ({
        ...previous,
        projects: previous.projects.map((item) => item.id === updated.id ? updated : item),
      }));
      onUpdated?.(updated);
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
    }
  }

  function updateVersion(action, message) {
    return updateProject(action, (updated) => {
      setOperations(currentOperations(updated));
      setTab('versions');
      setNotice({ text: `${message} v${currentVersion(updated).number}` });
    });
  }

  return (
    <div className="app">
      <Header
        user={data.user}
        usedBytes={data.usedBytes}
        quotaBytes={data.quotaBytes}
        onReturn={owner ? returnToOwnProjects : undefined}
        busy={busy}
      />
      <ProjectList owner={owner} projects={projects} projectId={projectId} onSelect={openProject} busy={busy} />
      <Workspace
        project={project}
        owner={owner}
        operations={owner && project ? currentOperations(project) : operations}
        setOperations={setOperations}
        busy={busy}
        onReset={reset}
        onLike={() => updateProject(() => setProjectLike(projectId, !project.likedByMe))}
        onSave={() => updateVersion(
          () => saveVersion(projectId, changedOperations(operations)),
          'Сохранена версия',
        )}
      />
      <EditorSidebar
        project={project}
        friends={data.friends}
        activeFriendId={owner?.id}
        onFriendSelect={openFriend}
        operations={operations}
        onAdjust={(key, value) => setOperations((previous) => ({ ...previous, [key]: value }))}
        onRestore={(id) => updateVersion(() => restoreVersion(projectId, id), 'Открыта версия')}
        tab={owner ? 'friends' : tab}
        onTabChange={setTab}
        busy={busy}
        error={error}
      />
      <div className={notice ? 'toast show' : 'toast'} role="status">{notice?.text}</div>
    </div>
  );
}

export function App() {
  const [result, setResult] = useState(null);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    loadEditor(controller.signal).then(
      (data) => setResult({ data }),
      (error) => {
        if (!controller.signal.aborted) setResult({ error: error.message });
      },
    );
    return () => controller.abort();
  }, [attempt]);

  if (result?.data) return <Editor initialData={result.data} />;
  if (!result) return <main className="page-status" role="status">Загрузка редактора…</main>;

  return (
    <main className="page-status">
      <p role="alert">{result.error}</p>
      <button className="btn" onClick={() => {
        setResult(null);
        setAttempt((value) => value + 1);
      }}>
        Повторить
      </button>
    </main>
  );
}
