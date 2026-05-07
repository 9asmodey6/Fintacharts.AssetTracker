import { useEffect, useState, useMemo, useRef } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router';
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts';
import { fetchPriceHistory } from '../api/assetApi';
import type { Asset, Price, PriceHistoryItem } from '../types';

interface AssetDetailPageProps {
    prices: Record<string, Price>;
}

export function AssetDetailPage({ prices }: AssetDetailPageProps) {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const location = useLocation();
    const asset = location.state?.asset as Asset | undefined;

    const [history, setHistory] = useState<PriceHistoryItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [barsCount, setBarsCount] = useState<number>(10);

    const livePrice = id ? prices[id] : undefined;

    // Track price direction locally
    const prevPriceRef = useRef<number | undefined>(undefined);
    const [direction, setDirection] = useState<'up' | 'down' | undefined>(undefined);

    useEffect(() => {
        if (livePrice) {
            if (prevPriceRef.current !== undefined && livePrice.last !== prevPriceRef.current) {
                setDirection(livePrice.last > prevPriceRef.current ? 'up' : 'down');
                
                const timer = setTimeout(() => {
                    setDirection(undefined);
                }, 800);
                
                prevPriceRef.current = livePrice.last;
                return () => clearTimeout(timer);
            }
            prevPriceRef.current = livePrice.last;
        }
    }, [livePrice?.last]);

    useEffect(() => {
        if (!id) return;
        
        setLoading(true);
        setError(null);
        
        fetchPriceHistory(id, barsCount)
            .then(res => {
                // Assuming history is returned in chronological order, 
                // if not, we should sort it here by timestamp.
                setHistory(res.history);
                setLoading(false);
            })
            .catch(err => {
                setError(err.message);
                setLoading(false);
            });
    }, [id, barsCount]);

    const chartData = useMemo(() => {
        return history.map(item => ({
            time: new Date(item.timestamp).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' }),
            price: item.price
        }));
    }, [history]);

    // Format symbol: "EURUSD" → "EUR / USD"
    const fmtSymbol = (sym: string | undefined) => {
        if (!sym) return '—';
        if (sym.length === 6) return `${sym.slice(0, 3)} / ${sym.slice(3)}`;
        return sym;
    };

    const fmt = (val: number | undefined) => {
        if (val === undefined || val === 0) return '—';
        if (val < 1) return val.toFixed(5);
        if (val < 100) return val.toFixed(4);
        return val.toFixed(2);
    };

    if (!asset && !id) {
        return <div className="error-message">Asset not found.</div>;
    }

    return (
        <div className="detail-page">
            <div className="detail-header">
                <button className="back-btn" onClick={() => navigate(-1)}>
                    ← Back
                </button>
                <div className="detail-title">
                    <h2>{fmtSymbol(asset?.symbol)}</h2>
                    {asset && (
                        <div className="detail-badges">
                            <span className="kind-badge">{asset.kind}</span>
                            <span className="provider-badge">{asset.provider}</span>
                        </div>
                    )}
                </div>
            </div>

            <div className="price-display">
                <div className="current-price">
                    <div className="price-label">Current Price</div>
                    <div className={`price-main ${direction === 'up' ? 'text-green' : direction === 'down' ? 'text-red' : ''}`}>
                        {fmt(livePrice?.last)}
                    </div>
                </div>
                <div className="price-details">
                    <div><span className="label">Bid:</span> <span className="price-value bid">{fmt(livePrice?.bid)}</span></div>
                    <div><span className="label">Ask:</span> <span className="price-value ask">{fmt(livePrice?.ask)}</span></div>
                </div>
            </div>

            <div className="chart-section">
                <div className="chart-controls">
                    <span className="period-label">Period:</span>
                    {[5, 10, 15].map(count => (
                        <button
                            key={count}
                            className={`period-btn ${barsCount === count ? 'active' : ''}`}
                            onClick={() => setBarsCount(count)}
                        >
                            {count}
                        </button>
                    ))}
                </div>

                <div className="chart-container">
                    {loading ? (
                        <div className="loading-container">
                            <div className="spinner" />
                        </div>
                    ) : error ? (
                        <div className="error-message">
                            <div className="error-icon">⚠️</div>
                            <div className="error-content">
                                <strong>Data Unavailable</strong>
                                <span>{error}</span>
                            </div>
                        </div>
                    ) : (
                        <ResponsiveContainer width="100%" height={300}>
                            <LineChart data={chartData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.1)" vertical={false} />
                                <XAxis 
                                    dataKey="time" 
                                    stroke="var(--text-muted)" 
                                    tick={{ fill: 'var(--text-muted)', fontSize: 12 }} 
                                    tickLine={false}
                                    axisLine={false}
                                />
                                <YAxis 
                                    domain={['auto', 'auto']} 
                                    stroke="var(--text-muted)" 
                                    tick={{ fill: 'var(--text-muted)', fontSize: 12 }} 
                                    tickLine={false}
                                    axisLine={false}
                                    tickFormatter={(val) => fmt(val)}
                                    width={80}
                                />
                                <Tooltip 
                                    contentStyle={{ backgroundColor: 'var(--bg-card)', borderColor: 'var(--border)', borderRadius: 'var(--radius-sm)' }}
                                    itemStyle={{ color: 'var(--accent)' }}
                                />
                                <Line 
                                    type="monotone" 
                                    dataKey="price" 
                                    stroke="url(#colorPrice)" 
                                    strokeWidth={3} 
                                    dot={{ fill: 'var(--bg-card)', stroke: 'var(--accent)', strokeWidth: 2, r: 4 }} 
                                    activeDot={{ r: 6, fill: 'var(--accent)', stroke: 'white', strokeWidth: 2 }}
                                    style={{ filter: 'drop-shadow(0px 4px 8px rgba(99, 102, 241, 0.3))' }}
                                />
                                <defs>
                                    <linearGradient id="colorPrice" x1="0" y1="0" x2="1" y2="0">
                                        <stop offset="0%" stopColor="var(--accent)" stopOpacity={0.6}/>
                                        <stop offset="100%" stopColor="var(--accent)" stopOpacity={1}/>
                                    </linearGradient>
                                </defs>
                            </LineChart>
                        </ResponsiveContainer>
                    )}
                </div>
            </div>
        </div>
    );
}
