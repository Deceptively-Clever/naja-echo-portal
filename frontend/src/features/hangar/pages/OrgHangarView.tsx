import { useState } from 'react'
import { useOrgHangar } from '../hooks/useOrgHangar'
import { useOwningMembers } from '../hooks/useOwningMembers'
import { ShipCardGallery } from '../components/ShipCardGallery'
import { OwnerCountBadge } from '../components/OwnerCountBadge'
import type { OrgHangarShipCard } from '../schemas/hangarShipCard'

export function OrgHangarView() {
  const [search, setSearch] = useState('')
  const [mine, setMine] = useState(false)
  const [memberId, setMemberId] = useState<string | undefined>(undefined)
  const [sortBy, setSortBy] = useState<'ownerCount' | 'name'>('ownerCount')

  const { data, isLoading, fetchNextPage, hasNextPage } = useOrgHangar(search || undefined, mine, memberId, sortBy)
  const { data: members } = useOwningMembers()

  const ships = data?.pages.flatMap((p) => p.items) ?? []

  const handleMemberChange = (id: string) => {
    if (id === '') {
      setMemberId(undefined)
    } else {
      setMemberId(id)
      setMine(false)
    }
  }

  const handleMineToggle = () => {
    if (!mine) {
      setMemberId(undefined)
    }
    setMine((prev) => !prev)
  }

  const emptyMessage = search
    ? 'No ships match your search.'
    : memberId
    ? 'No ships found for that member.'
    : 'No members have added any ships yet.'

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-bold">Org Hangar</h1>

      <ShipCardGallery
        ships={ships}
        search={search}
        onSearchChange={setSearch}
        emptyStateMessage={emptyMessage}
        isLoading={isLoading}
        onLoadMore={fetchNextPage}
        hasMore={hasNextPage}
        renderBadge={(ship) => {
          const orgShip = ship as OrgHangarShipCard
          return orgShip.owners ? (
            <OwnerCountBadge ownerCount={orgShip.ownerCount} owners={orgShip.owners} />
          ) : null
        }}
        headerSlot={
          <div className="flex gap-2 items-center flex-wrap">
            {/* My Ships toggle */}
            <button
              onClick={handleMineToggle}
              aria-pressed={mine}
              className={`px-3 py-1 text-xs rounded-full border transition-colors ${
                mine
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'border-border text-muted-foreground hover:text-foreground'
              }`}
            >
              My Ships
            </button>

            {/* Member filter */}
            <select
              value={memberId ?? ''}
              onChange={(e) => handleMemberChange(e.target.value)}
              className="h-7 rounded-md border border-input bg-background px-2 text-xs text-foreground"
              aria-label="Filter by member"
            >
              <option value="">All Members</option>
              {(members ?? []).map((m) => (
                <option key={m.userId} value={m.userId}>
                  {m.displayName}
                </option>
              ))}
            </select>

            {/* Sort control */}
            <select
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value as 'ownerCount' | 'name')}
              className="h-7 rounded-md border border-input bg-background px-2 text-xs text-foreground"
              aria-label="Sort by"
            >
              <option value="ownerCount">Most Owners</option>
              <option value="name">Ship Name</option>
            </select>
          </div>
        }
      />
    </div>
  )
}
