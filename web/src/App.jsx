import { useEffect, useState } from 'react';
import { EditorSidebar, Identity } from './EditorSidebar.jsx';
import { loadEditor, restoreVersion, saveVersion } from './editorApi.js';
import { changedOperations, defaults, imageFilter } from './operations.js';

function megabytes(bytes) {
  return `${(bytes / 1_000_000).toFixed(1)} МБ`;
}

function currentOperations(project) {
  const version = project.versions.find((item) => item.id === project.currentVersionId);
  return { ...defaults, ...version?.operations };
}

function Header({ user, usedBytes, quotaBytes }) {
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
      <button className="btn" onClick={toggleTheme} aria-label="Сменить тему">Тема</button>
    </header>
  );
}

function ProjectList({ projects, projectId, onSelect, busy }) {
  return (
    <aside aria-label="Проекты">
      <h3>Проекты</h3>
      {projects.length === 0 && <p>Проектов пока нет</p>}
      {projects.map(({ id, image, versions }) => (
        <button
          key={id}
          className={id === projectId ? 'proj on' : 'proj'}
          aria-pressed={id === projectId}
          disabled={busy}
          onClick={() => onSelect(id)}
        >
          <img src={image.image_link} alt="" />
          <span>Проект #{id}<small>{versions.length} верс. · {megabytes(image.size)}</small></span>
        </button>
      ))}
    </aside>
  );
}

function Workspace({ project, operations, setOperations, onSave, onReset, busy }) {
  if (!project) return <main className="page-status">Выберите проект, чтобы открыть редактор</main>;

  const { image, currentVersionId } = project;
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
        <button className="btn" disabled={busy} onClick={() => turn(-90)}>Повернуть ⟲</button>
        <button className="btn" disabled={busy} onClick={() => turn(90)}>Повернуть ⟳</button>
        <button className="btn" disabled={busy} onClick={flip} aria-pressed={flipX}>Отразить</button>
        <button className="btn" disabled={busy} onClick={onReset}>Сбросить</button>
        <span className="sp" />
        <button className="btn pri" disabled={busy} onClick={onSave}>Сохранить версию</button>
      </div>
      <div className="stage">
        <img src={image.image_link} alt="Редактируемое изображение" style={style} />
      </div>
      <div className="meta">
        <span>{image.width}×{image.height}</span>
        <span>{megabytes(image.size)}</span>
        {currentVersionId != null && <span>Версия v{currentVersionId}</span>}
      </div>
    </main>
  );
}

function Editor({ initialData }) {
  const [data, setData] = useState(initialData);
  const [projectId, setProjectId] = useState(() => initialData.projects[0]?.id);
  const [operations, setOperations] = useState(() => (
    initialData.projects.length ? currentOperations(initialData.projects[0]) : defaults
  ));
  const [tab, setTab] = useState('adjustments');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(null);
  const project = data.projects.find((item) => item.id === projectId);

  useEffect(() => {
    if (!notice) return;
    const timeout = window.setTimeout(() => setNotice(null), 1800);
    return () => window.clearTimeout(timeout);
  }, [notice]);

  function openProject(id) {
    setProjectId(id);
    setOperations(currentOperations(data.projects.find((item) => item.id === id)));
    setError('');
  }

  function reset() {
    setOperations(defaults);
    setNotice({ text: 'Правки сброшены' });
  }

  async function updateProject(action, message) {
    setBusy(true);
    setError('');
    try {
      const updated = await action();
      setData((previous) => ({
        ...previous,
        projects: previous.projects.map((item) => item.id === updated.id ? updated : item),
      }));
      setOperations(currentOperations(updated));
      setTab('versions');
      setNotice({ text: `${message} v${updated.currentVersionId}` });
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="app">
      <Header user={data.user} usedBytes={data.usedBytes} quotaBytes={data.quotaBytes} />
      <ProjectList projects={data.projects} projectId={projectId} onSelect={openProject} busy={busy} />
      <Workspace
        project={project}
        operations={operations}
        setOperations={setOperations}
        busy={busy}
        onReset={reset}
        onSave={() => updateProject(
          () => saveVersion(projectId, changedOperations(operations)),
          'Сохранена версия',
        )}
      />
      <EditorSidebar
        project={project}
        friends={data.friends}
        operations={operations}
        onAdjust={(key, value) => setOperations((previous) => ({ ...previous, [key]: value }))}
        onRestore={(id) => updateProject(() => restoreVersion(projectId, id), 'Открыта версия')}
        tab={tab}
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
