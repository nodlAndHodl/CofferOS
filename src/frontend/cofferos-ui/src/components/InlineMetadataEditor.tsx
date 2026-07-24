import { useState } from 'react';
import { Check, Edit2, X } from 'lucide-react';
import { api } from '../api/client';
import { Badge } from './ui';
import { getTagColorClass } from '../lib/tagColor';

interface InlineMetadataEditorProps {
  walletId: string;
  target: string;
  reference: string;
  label?: string | null;
  category?: string | null;
  tags: string[];
  onSaved?: () => void;
}

export function InlineMetadataEditor({
  walletId,
  target,
  reference,
  label,
  category,
  tags,
  onSaved,
}: InlineMetadataEditorProps) {
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [labelInput, setLabelInput] = useState(label ?? '');
  const [categoryInput, setCategoryInput] = useState(category ?? '');
  const [tagsInput, setTagsInput] = useState(tags.join(', '));

  async function handleSave() {
    setSaving(true);
    try {
      const parsedTags = tagsInput
        .split(/[,\n]+/)
        .map((t) => t.trim().toLowerCase())
        .filter((t) => t.length > 0);

      await api.updateObjectMetadata(walletId, target, reference, {
        target,
        reference,
        label: labelInput.trim() || null,
        category: categoryInput.trim() || null,
        tags: parsedTags,
        metadata: {},
      });

      setEditing(false);
      onSaved?.();
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Failed to save');
    } finally {
      setSaving(false);
    }
  }

  function handleCancel() {
    setLabelInput(label ?? '');
    setCategoryInput(category ?? '');
    setTagsInput(tags.join(', '));
    setEditing(false);
  }

  if (editing) {
    return (
      <div className="flex flex-col gap-2 rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] p-2">
        <input
          type="text"
          value={labelInput}
          onChange={(e) => setLabelInput(e.target.value.replace(/[,\n]+/g, ''))}
          placeholder="Label"
          className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-2 py-1 text-xs text-white outline-none focus:border-[var(--color-coffer-orange)]"
        />
        <input
          type="text"
          value={categoryInput}
          onChange={(e) => setCategoryInput(e.target.value.replace(/[,\n]+/g, ''))}
          placeholder="Category"
          className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-2 py-1 text-xs text-white outline-none focus:border-[var(--color-coffer-orange)]"
        />
        <input
          type="text"
          value={tagsInput}
          onChange={(e) => setTagsInput(e.target.value)}
          placeholder="tag1, tag2, tag3"
          className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-2 py-1 text-xs text-white outline-none focus:border-[var(--color-coffer-orange)]"
        />
        <div className="flex items-center gap-1">
          <button
            onClick={handleSave}
            disabled={saving}
            className="rounded p-1 text-green-400 hover:bg-green-400/10 disabled:opacity-40"
            title="Save"
          >
            <Check size={14} />
          </button>
          <button
            onClick={handleCancel}
            disabled={saving}
            className="rounded p-1 text-red-400 hover:bg-red-400/10 disabled:opacity-40"
            title="Cancel"
          >
            <X size={14} />
          </button>
        </div>
      </div>
    );
  }

  const labels = label?.trim() ? [label.trim()] : [];
  const categories = category?.trim() ? [category.trim()] : [];
  const hasLabelOrCategory = labels.length > 0 || categories.length > 0;

  return (
    <div className="flex flex-wrap items-center gap-1">
      {labels.map((l) => (
        <Badge key={l} tone="blue">
          {l}
        </Badge>
      ))}
      {categories.map((c) => (
        <Badge key={c} tone="purple">
          {c}
        </Badge>
      ))}
      {tags.map((tag) => (
        <Badge key={tag} className={getTagColorClass(tag)}>
          {tag}
        </Badge>
      ))}
      <button
        onClick={() => setEditing(true)}
        className={`rounded p-1 ${hasLabelOrCategory || tags.length > 0 ? 'text-[var(--color-coffer-orange)]' : 'text-[var(--color-coffer-muted)]'} hover:text-[var(--color-coffer-orange)]`}
        title={hasLabelOrCategory || tags.length > 0 ? 'Edit metadata' : 'Add metadata'}
      >
        <Edit2 size={14} />
      </button>
    </div>
  );
}
