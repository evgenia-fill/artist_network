import { useEffect, useState } from 'react';
import { getToken } from './auth';

export default function AuthenticatedImage({ src, alt = '', ...props }) {
    const [imageUrl, setImageUrl] = useState('');

    useEffect(() => {
        if (!src) {
            setImageUrl('');
            return;
        }

        let objectUrl;

        const controller = new AbortController();

        async function loadImage() {
            try {
                const token = getToken();

                const response = await fetch(src, {
                    headers: token
                        ? { Authorization: `Bearer ${token}` }
                        : {},
                    signal: controller.signal
                });

                if (!response.ok) {
                    throw new Error(`Ошибка загрузки изображения: ${response.status}`);
                }

                const blob = await response.blob();
                objectUrl = URL.createObjectURL(blob);
                setImageUrl(objectUrl);
            } catch (error) {
                if (error.name !== 'AbortError') {
                    console.error('Не удалось загрузить изображение:', error);
                }
            }
        }

        loadImage();

        return () => {
            controller.abort();
            if (objectUrl) URL.revokeObjectURL(objectUrl);
        };
    }, [src]);

    if (!imageUrl) return null;

    return <img src={imageUrl} alt={alt} {...props} />;
}
