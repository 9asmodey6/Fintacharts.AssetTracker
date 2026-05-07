const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5286';

import type { GetAssetsResponse, GetPriceHistoryResponse } from '../types';

async function handleApiError(response: Response): Promise<never> {
    try {
        const data = await response.json();
        if (data && data.title) {
            let message = data.detail || data.title;
            if (data.errors) {
                const validationErrors = Object.values(data.errors).flat().join(' | ');
                if (validationErrors) {
                    message += ` (${validationErrors})`;
                }
            }
            throw new Error(message);
        }
    } catch (e) {
        if (e instanceof Error && e.message !== 'Unexpected end of JSON input') {
            throw e;
        }
    }
    throw new Error(`Server returned status: ${response.status} ${response.statusText}`);
}

export async function fetchAssets(): Promise<GetAssetsResponse> {
    const response = await fetch(`${API_BASE}/api/assets`);

    if (!response.ok) {
        await handleApiError(response);
    }

    return response.json();
}

export async function fetchPriceHistory(id: string, barsCount: number = 10): Promise<GetPriceHistoryResponse> {
    const response = await fetch(`${API_BASE}/api/assets/${id}/history?barsCount=${barsCount}`);

    if (!response.ok) {
        await handleApiError(response);
    }

    return response.json();
}
