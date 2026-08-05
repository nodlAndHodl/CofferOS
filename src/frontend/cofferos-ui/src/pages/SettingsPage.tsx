import { Card } from '../components/ui';

export function SettingsPage() {
  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-bold">Settings</h1>
        <p className="text-sm text-[var(--color-coffer-muted)]">Application configuration</p>
      </div>

      <Card className="p-6 text-sm text-[var(--color-coffer-muted)]">
        <p>Settings will be available in a future release.</p>
        <p className="mt-2">Configuration is currently managed through environment variables and the docker-compose file.</p>
      </Card>
    </div>
  );
}
