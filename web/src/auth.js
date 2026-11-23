
const TOKEN_KEY = 'pixelflow.auth.token';
const USER_KEY = 'pixelflow.auth.user';

export function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}

export function getAuthUser() {
    try {
        return JSON.parse(localStorage.getItem(USER_KEY) || 'null');
    } catch {
        return null;
    }
}

export function saveSession(session) {
    if (!session?.token || !session?.user?.id) {
        throw new Error('Сервер вернул некорректный ответ');
    }

    localStorage.setItem(TOKEN_KEY, session.token);

    localStorage.setItem(
        USER_KEY,
        JSON.stringify({
            id: session.user.id,
            login: session.user.login,
            name: session.user.login,
        })
    );

    return getAuthUser();
}

export function clearSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
}

async function postAuth(url, login, password) {
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ login, password }),
    });

    const data = await response.json().catch(() => ({}));

    if (!response.ok) {
        throw new Error(
            data.message ||
            data.title ||
            data.error ||
            `Ошибка сервера: ${response.status}`
        );
    }

    return saveSession(data);
}

export function login(login, password) {
    return postAuth('/api/auth/login', login.trim(), password);
}

export function register(login, password) {
    return postAuth('/api/auth/register', login.trim(), password);
}
