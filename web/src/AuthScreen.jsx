import { useState } from 'react';
import { login, register } from './auth.js';

export default function AuthScreen({ onAuthenticated }) {
    const [mode, setMode] = useState('login');
    const [loginValue, setLoginValue] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    async function handleSubmit(event) {
        event.preventDefault();
        setError('');
        setLoading(true);

        try {
            const user = mode === 'login'
                ? await login(loginValue, password)
                : await register(loginValue, password);

            onAuthenticated(user);
        } catch (err) {
            setError(err.message || 'Не удалось выполнить запрос');
        } finally {
            setLoading(false);
        }
    }

    return (
        <main className="auth-page">
            <form className="auth-card" onSubmit={handleSubmit}>
                <h1>PixelFlow</h1>

                <h2>
                    {mode === 'login' ? 'Вход в аккаунт' : 'Регистрация'}
                </h2>

                <label htmlFor="login">Логин</label>
                <input
                    id="login"
                    value={loginValue}
                    onChange={e => setLoginValue(e.target.value)}
                    autoComplete="username"
                    required
                />

                <label htmlFor="password">Пароль</label>
                <input
                    id="password"
                    type="password"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    autoComplete={
                        mode === 'login' ? 'current-password' : 'new-password'
                    }
                    minLength={8}
                    required
                />

                {error && <p className="auth-error">{error}</p>}

                <button className="btn pri" type="submit" disabled={loading}>
                    {loading
                        ? 'Подождите...'
                        : mode === 'login' ? 'Войти' : 'Зарегистрироваться'}
                </button>

                <p>
                    {mode === 'login' ? 'Нет аккаунта?' : 'Уже зарегистрированы?'}
                    {' '}
                    <button
                        type="button"
                        className="auth-link"
                        onClick={() => {
                            setMode(mode === 'login' ? 'register' : 'login');
                            setError('');
                        }}
                    >
                        {mode === 'login' ? 'Регистрация' : 'Войти'}
                    </button>
                </p>
            </form>
        </main>
    );
}
