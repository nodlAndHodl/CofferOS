import ReactDOM from 'react-dom/client';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import './index.css';
import { Layout } from './components/Layout';
import { DashboardPage } from './pages/DashboardPage';
import { HoldingsPage } from './pages/HoldingsPage';
import { WalletsPage } from './pages/WalletsPage';
import { WalletDetailPage } from './pages/WalletDetailPage';
import { TreasuryPage } from './pages/TreasuryPage';
import { LoanDetailPage } from './pages/LoanDetailPage';
import { InfrastructurePage } from './pages/InfrastructurePage';
import { RetirementAccountsPage } from './pages/RetirementAccountsPage';
import { RetirementAccountDetailPage } from './pages/RetirementAccountDetailPage';
import { SettingsPage } from './pages/SettingsPage';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <BrowserRouter>
    <Layout>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/holdings" element={<HoldingsPage />} />
        <Route path="/holdings/wallets" element={<WalletsPage />} />
        <Route path="/holdings/retirement" element={<RetirementAccountsPage />} />
        <Route path="/holdings/retirement/:id" element={<RetirementAccountDetailPage />} />
        <Route path="/wallets" element={<WalletsPage />} />
        <Route path="/wallets/:id" element={<WalletDetailPage />} />
        <Route path="/treasury" element={<TreasuryPage />} />
        <Route path="/treasury/:id" element={<LoanDetailPage />} />
        <Route path="/infrastructure" element={<InfrastructurePage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Routes>
    </Layout>
  </BrowserRouter>,
);
