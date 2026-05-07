import { useEffect, useRef, useState } from 'react';
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import type { Price } from '../types';

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5286';

export function useSignalR() {
    const [prices, setPrices] = useState<Record<string, Price>>({});
    const [connected, setConnected] = useState(false);
    const connectionRef = useRef<HubConnection | null>(null);

    useEffect(() => {
        const connection = new HubConnectionBuilder()
            .withUrl(`${API_BASE}/hubs/prices`)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        connectionRef.current = connection;

        connection.on('PriceUpdated', (price: Price) => {
            setPrices(prev => ({
                ...prev,
                [price.instrumentId]: price,
            }));
        });

        connection.onreconnecting(() => setConnected(false));
        connection.onreconnected(() => setConnected(true));
        connection.onclose(() => setConnected(false));

        connection
            .start()
            .then(() => setConnected(true))
            .catch(err => console.error('SignalR connection failed:', err));

        return () => {
            connection.stop();
        };
    }, []);

    return { prices, connected };
}
