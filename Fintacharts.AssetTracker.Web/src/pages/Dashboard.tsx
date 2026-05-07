import { useEffect, useMemo, useState, useRef } from 'react';
import { useNavigate } from 'react-router';
import type { Asset, Price } from '../types';

interface DashboardProps {
    assets: Asset[];
    prices: Record<string, Price>;
}

export function Dashboard({ assets, prices }: DashboardProps) {
    const [search, setSearch] = useState('');
    const navigate = useNavigate();

    // Track previous prices for flash animation
    const prevPricesRef = useRef<Record<string, number>>({});
    const [flashMap, setFlashMap] = useState<Record<string, 'green' | 'red'>>({});
    const [directionMap, setDirectionMap] = useState<Record<string, 'up' | 'down'>>({});

    // Detect price changes and trigger flash
    useEffect(() => {
        const newFlashes: Record<string, 'green' | 'red'> = {};
        const newDirections: Record<string, 'up' | 'down'> = {};
        for (const [id, price] of Object.entries(prices)) {
            const prevLast = prevPricesRef.current[id];
            if (prevLast !== undefined && price.last !== prevLast) {
                newFlashes[id] = price.last > prevLast ? 'green' : 'red';
            }
            if (prevLast !== undefined && price.last !== prevLast) {
                newDirections[id] = price.last > prevLast ? 'up' : 'down';
            }
            prevPricesRef.current[id] = price.last;
        }

        if (Object.keys(newFlashes).length > 0) {
            setFlashMap(prev => ({ ...prev, ...newFlashes }));
            setDirectionMap(prev => ({ ...prev, ...newDirections }));
            // Clear flashes after animation completes
            const timer = setTimeout(() => {
                setFlashMap(prev => {
                    const next = { ...prev };
                    for (const id of Object.keys(newFlashes)) {
                        delete next[id];
                    }
                    return next;
                });
            }, 800);
            return () => clearTimeout(timer);
        }
    }, [prices]);

    // Filter assets by search
    const filtered = useMemo(() => {
        if (!search.trim()) return assets;
        const q = search.toLowerCase();
        return assets.filter(a =>
            a.symbol.toLowerCase().includes(q) ||
            (a.description?.toLowerCase().includes(q))
        );
    }, [assets, search]);

    // Count how many assets have live prices
    const liveCount = Object.keys(prices).length;

    // Format symbol: "EURUSD" → "EUR / USD"
    const fmtSymbol = (sym: string) => {
        if (sym.length === 6) return `${sym.slice(0, 3)} / ${sym.slice(3)}`;
        return sym;
    };

    // Format price to fixed decimals
    const fmt = (val: number | undefined) => {
        if (val === undefined || val === 0) return '—';
        if (val < 1) return val.toFixed(5);
        if (val < 100) return val.toFixed(4);
        return val.toFixed(2);
    };

    // Format time
    const fmtTime = (iso: string | undefined) => {
        if (!iso) return '—';
        const d = new Date(iso);
        return d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
    };

    return (
        <>
            {/* Stats */}
            <div className="stats-bar">
                <div className="stat-card">
                    <div className="stat-label">Total Instruments</div>
                    <div className="stat-value">{assets.length}</div>
                </div>
                <div className="stat-card">
                    <div className="stat-label">Live Prices</div>
                    <div className="stat-value accent">{liveCount}</div>
                </div>
                <div className="stat-card">
                    <div className="stat-label">Providers</div>
                    <div className="stat-value">
                        {new Set(assets.map(a => a.provider)).size}
                    </div>
                </div>
                <div className="stat-card">
                    <div className="stat-label">Displayed</div>
                    <div className="stat-value">{filtered.length}</div>
                </div>
            </div>

            {/* Search */}
            <div className="search-wrapper">
                <span className="search-icon">🔍</span>
                <input
                    id="search"
                    className="search-input"
                    type="text"
                    placeholder="Search by symbol or description…"
                    value={search}
                    onChange={e => setSearch(e.target.value)}
                />
            </div>

            {/* Table */}
            <div className="table-container">
                <table className="data-table">
                    <thead>
                    <tr>
                        <th>Symbol</th>
                        <th>Type</th>
                        <th>Provider</th>
                        <th className="text-right">Bid</th>
                        <th className="text-right">Ask</th>
                        <th className="text-right">Last</th>
                        <th className="text-right">Spread</th>
                        <th className="text-right">Updated</th>
                    </tr>
                    </thead>
                    <tbody>
                    {filtered.length === 0 ? (
                        <tr>
                            <td colSpan={8} className="empty-state">
                                No instruments match your search.
                            </td>
                        </tr>
                    ) : (
                        filtered.map(asset => {
                            const p = prices[asset.id];
                            const flash = flashMap[asset.id];
                            const spread = p && p.ask > 0 && p.bid > 0
                                ? (p.ask - p.bid)
                                : undefined;

                            return (
                                <tr
                                    key={asset.id}
                                    className={`clickable-row ${flash ? `flash-${flash}` : ''}`}
                                    onClick={() => navigate(`/asset/${asset.id}`, { state: { asset } })}
                                >
                                    <td>
                                        <div className="symbol-cell">
                                            <span className={`symbol-dot ${p ? 'live' : ''}`} />
                                            <div>
                                                <div className="symbol-name">{fmtSymbol(asset.symbol)}</div>
                                                {asset.description && (
                                                    <div className="symbol-desc">{asset.description}</div>
                                                )}
                                            </div>
                                        </div>
                                    </td>
                                    <td>
                                        <span className="kind-badge">{asset.kind}</span>
                                    </td>
                                    <td style={{ color: 'var(--text-secondary)' }}>{asset.provider}</td>
                                    <td className="text-right">
                                        <span className={`price-value bid ${!p ? 'no-data' : ''}`}>
                                            {fmt(p?.bid)}
                                        </span>
                                    </td>
                                    <td className="text-right">
                                        <span className={`price-value ask ${!p ? 'no-data' : ''}`}>
                                            {fmt(p?.ask)}
                                        </span>
                                    </td>
                                    <td className="text-right">
                                        <span className={`price-value ${!p ? 'no-data' : ''}`}>
                                            {directionMap[asset.id] === 'up' && <span className="arrow up">▲</span>}
                                            {directionMap[asset.id] === 'down' && <span className="arrow down">▼</span>}
                                            {fmt(p?.last)}
                                        </span>
                                    </td>
                                    <td className="text-right">
                                        <span className="spread-value">
                                            {spread !== undefined ? fmt(spread) : '—'}
                                        </span>
                                    </td>
                                    <td className="text-right">
                                        <span className="time-value">
                                            {fmtTime(p?.updatedAt)}
                                        </span>
                                    </td>
                                </tr>
                            );
                        })
                    )}
                    </tbody>
                </table>
            </div>
        </>
    );
}
