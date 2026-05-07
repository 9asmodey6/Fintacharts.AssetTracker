const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5286';

import type { GetAssetsResponse, GetPriceHistoryResponse } from '../types';

export async function fetchAssets(): Promise<GetAssetsResponse> {
    const response = await fetch(`${API_BASE}/api/assets`);

    if (!response.ok) {
        throw new Error(`Failed to fetch assets: ${response.status}`);
    }

    return response.json();
}

export async function fetchPriceHistory(id: string, barsCount: number = 10): Promise<GetPriceHistoryResponse> {
    const response = await fetch(`${API_BASE}/api/assets/${id}/history?barsCount=${barsCount}`);

    if (!response.ok) {
        throw new Error(`Failed to fetch history: ${response.status}`);
    }

    return response.json();
}
