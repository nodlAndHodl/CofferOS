import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import './index.css';
import { Layout } from './components/Layout';
import { DashboardPage } from './pages/DashboardPage';
import { WalletDetailPage } from './pages/WalletDetailPage';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <Layout>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/wallets/:id" element={<WalletDetailPage />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  </React.StrictMode>,
);
