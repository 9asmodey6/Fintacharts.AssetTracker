import { useEffect, useState } from 'react';
import { Routes, Route } from 'react-router';
import { fetchAssets } from './api/assetApi';
import { useSignalR } from './hooks/useSignalR';
import type { Asset } from './types';
import { Dashboard } from './pages/Dashboard';
import { AssetDetailPage } from './pages/AssetDetailPage';
import './App.css';

function App() {
    const [assets, setAssets] = useState<Asset[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const { prices, connected } = useSignalR();

    useEffect(() => {
        fetchAssets()
            .then(data => {
                setAssets(data.assets);
                setLoading(false);
            })
            .catch(err => {
                setError(err.message);
                setLoading(false);
            });
    }, []);

    return (
        <div className="app">
            {/* Topbar */}
            <header className="header">
                <div className="header-inner">
                    <div className="header-title">
                        <div className="logo-icon">
                            <img src="/logo.png" alt="Logo" />
                        </div>
                        <h1>Asset Tracker</h1>
                    </div>
                    <div className={`status-badge ${connected ? 'connected' : 'disconnected'}`}>
                        <span className="status-dot" />
                        {connected ? 'Live' : 'Connecting...'}
                    </div>
                </div>
            </header>

            {/* Main Content Area with Routing */}
            <main className="main-content">
                {loading ? (
                    <div className="loading-container">
                        <div className="spinner" />
                        <p>Loading instruments...</p>
                    </div>
                ) : error ? (
                    <div className="error-container">
                        <div className="error-icon">⚠️</div>
                        <div className="error-message">{error}</div>
                    </div>
                ) : (
                    <Routes>
                        <Route 
                            path="/" 
                            element={<Dashboard assets={assets} prices={prices} />} 
                        />
                        <Route 
                            path="/asset/:id" 
                            element={<AssetDetailPage prices={prices} />} 
                        />
                    </Routes>
                )}
            </main>

            {/* Footer */}
            <footer className="footer">
                Fintacharts Asset Tracker • Real-time market data
            </footer>
        </div>
    );
}

export default App;
