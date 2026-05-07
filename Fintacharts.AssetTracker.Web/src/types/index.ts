
export interface Asset {
    id: string;
    symbol: string;
    description: string | null;
    kind: string;
    provider: string;
}


export interface GetAssetsResponse {
    assets: Asset[];
}

export interface Price {
    instrumentId: string;
    bid: number;
    ask: number;
    last: number;
    updatedAt: string;
}
export interface PriceHistoryItem {
    timestamp: string;
    price: number;
}

export interface GetPriceHistoryResponse {
    history: PriceHistoryItem[];
}