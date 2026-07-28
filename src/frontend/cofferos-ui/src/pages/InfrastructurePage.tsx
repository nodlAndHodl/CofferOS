import { useEffect, useState } from 'react';
import { Server, Zap } from 'lucide-react';
import { api } from '../api/client';
import type { NodeStatus, ElectrumStatus } from '../types';
import { Card, Spinner } from '../components/ui';

export function InfrastructurePage() {
  const [node, setNode] = useState<NodeStatus | null>(null);
  const [nodeLoading, setNodeLoading] = useState(true);
  const [electrum, setElectrum] = useState<ElectrumStatus | null>(null);
  const [electrumLoading, setElectrumLoading] = useState(true);

  useEffect(() => {
    setNodeLoading(true);
    api.getNodeStatus()
      .then(setNode)
      .catch((e) => setNode({ connected: false, providerId: 'none', error: e instanceof Error ? e.message : 'Failed' }))
      .finally(() => setNodeLoading(false));
  }, []);

  useEffect(() => {
    setElectrumLoading(true);
    api.getElectrumStatus()
      .then(setElectrum)
      .catch((e) => setElectrum({ connected: false, providerId: 'electrum', host: '', port: 0, error: e instanceof Error ? e.message : 'Failed' }))
      .finally(() => setElectrumLoading(false));
  }, []);

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-bold">Infrastructure</h1>
        <p className="text-sm text-[var(--color-coffer-muted)]">Monitor your Bitcoin infrastructure services</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        {/* Node */}
        <Card className="p-5">
          <div className="mb-3 flex items-center gap-3">
            <div className="grid h-10 w-10 place-items-center rounded-lg bg-[var(--color-coffer-border)]">
              <Server size={20} />
            </div>
            <div>
              <div className="font-semibold">Bitcoin Node</div>
              <div className="text-xs text-[var(--color-coffer-muted)]">Full node connection</div>
            </div>
          </div>
          {nodeLoading ? (
            <Spinner />
          ) : node ? (
            <div className="space-y-2 text-sm">
              <div className="flex items-center gap-2">
                <span className={`h-2.5 w-2.5 rounded-full ${node.connected ? 'bg-emerald-400' : 'bg-red-400'}`} />
                <span>{node.connected ? 'Connected' : 'Disconnected'}</span>
              </div>
              {node.chain && <div className="text-[var(--color-coffer-muted)]">Chain: {node.chain}</div>}
              {node.blocks != null && <div className="text-[var(--color-coffer-muted)]">Block Height: {node.blocks.toLocaleString()}</div>}
              {node.error && <div className="text-red-400 text-xs">{node.error}</div>}
            </div>
          ) : (
            <div className="text-sm text-[var(--color-coffer-muted)]">Not configured</div>
          )}
        </Card>

        {/* Electrum */}
        <Card className="p-5">
          <div className="mb-3 flex items-center gap-3">
            <div className="grid h-10 w-10 place-items-center rounded-lg bg-[var(--color-coffer-border)]">
              <Zap size={20} />
            </div>
            <div>
              <div className="font-semibold">Electrum Server</div>
              <div className="text-xs text-[var(--color-coffer-muted)]">Address indexing</div>
            </div>
          </div>
          {electrumLoading ? (
            <Spinner />
          ) : electrum ? (
            <div className="space-y-2 text-sm">
              <div className="flex items-center gap-2">
                <span className={`h-2.5 w-2.5 rounded-full ${electrum.connected ? 'bg-emerald-400' : 'bg-red-400'}`} />
                <span>{electrum.connected ? 'Connected' : 'Disconnected'}</span>
              </div>
              {electrum.host && <div className="text-[var(--color-coffer-muted)]">Host: {electrum.host}:{electrum.port}</div>}
              {electrum.blockHeight != null && <div className="text-[var(--color-coffer-muted)]">Block Height: {electrum.blockHeight.toLocaleString()}</div>}
              {electrum.socks5Proxy && <div className="text-[var(--color-coffer-muted)]">Proxy: {electrum.socks5Proxy}</div>}
              {electrum.error && <div className="text-red-400 text-xs">{electrum.error}</div>}
            </div>
          ) : (
            <div className="text-sm text-[var(--color-coffer-muted)]">Not configured</div>
          )}
        </Card>
      </div>
    </div>
  );
}
