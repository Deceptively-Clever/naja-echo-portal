import { useState, useMemo } from 'react'
import type { CategoryListItem } from '../schemas/itemSchemas'

interface CategorySelectorProps {
  categories: CategoryListItem[]
  selectedId: number | undefined
  onSelect: (uexId: number) => void
}

export function CategorySelector({ categories, selectedId, onSelect }: CategorySelectorProps) {
  const [search, setSearch] = useState('')
  const [sectionFilter, setSectionFilter] = useState('')
  const [miningOnly, setMiningOnly] = useState(false)
  const [gameRelatedOnly, setGameRelatedOnly] = useState(false)

  const sections = useMemo(() => {
    const unique = new Set(categories.map((c) => c.section).filter(Boolean) as string[])
    return Array.from(unique).sort()
  }, [categories])

  const filtered = useMemo(() => {
    return categories.filter((c) => {
      if (search && !c.name.toLowerCase().includes(search.toLowerCase())) return false
      if (sectionFilter && c.section !== sectionFilter) return false
      if (miningOnly && !c.isMining) return false
      if (gameRelatedOnly && !c.isGameRelated) return false
      return true
    })
  }, [categories, search, sectionFilter, miningOnly, gameRelatedOnly])

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-2">
        <input
          type="text"
          placeholder="Search categories…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="border rounded px-2 py-1 text-sm bg-background"
        />

        <label htmlFor="section-filter" className="sr-only">Section</label>
        <select
          id="section-filter"
          aria-label="Section"
          value={sectionFilter}
          onChange={(e) => setSectionFilter(e.target.value)}
          className="border rounded px-2 py-1 text-sm bg-background"
        >
          <option value="">All sections</option>
          {sections.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>

        <label className="flex items-center gap-1 text-sm cursor-pointer">
          <input
            type="checkbox"
            aria-label="Mining only"
            checked={miningOnly}
            onChange={(e) => setMiningOnly(e.target.checked)}
          />
          Mining
        </label>

        <label className="flex items-center gap-1 text-sm cursor-pointer">
          <input
            type="checkbox"
            aria-label="Game-related only"
            checked={gameRelatedOnly}
            onChange={(e) => setGameRelatedOnly(e.target.checked)}
          />
          Game-related
        </label>
      </div>

      <div className="border rounded overflow-auto max-h-64">
        {filtered.length === 0 ? (
          <p className="p-3 text-sm text-muted-foreground">No categories match filters.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-left">
                <th className="px-3 py-2">Name</th>
                <th className="px-3 py-2">Section</th>
                <th className="px-3 py-2 text-right">Items</th>
                <th className="px-3 py-2">Last Imported</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((cat) => (
                <tr
                  key={cat.uexId}
                  onClick={() => onSelect(cat.uexId)}
                  className={`border-b cursor-pointer hover:bg-muted/30 ${selectedId === cat.uexId ? 'bg-muted/50' : ''}`}
                >
                  <td className="px-3 py-2">{cat.name}</td>
                  <td className="px-3 py-2 text-muted-foreground">{cat.section ?? '—'}</td>
                  <td className="px-3 py-2 text-right">{cat.localItemCount}</td>
                  <td className="px-3 py-2 text-muted-foreground">
                    {cat.lastImportedAt ? new Date(cat.lastImportedAt).toLocaleDateString() : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <p className="text-xs text-muted-foreground">
        {filtered.length} of {categories.length} {categories.length === 1 ? 'category' : 'categories'}
      </p>
    </div>
  )
}
