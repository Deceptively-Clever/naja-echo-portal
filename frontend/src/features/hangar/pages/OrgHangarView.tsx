import { useState } from 'react'
import { useOrgHangar } from '../hooks/useOrgHangar'
import { useOwningMembers } from '../hooks/useOwningMembers'
import { ShipCardGallery } from '../components/ShipCardGallery'
import { OwnerCountBadge } from '../components/OwnerCountBadge'
import { Combobox } from '@/components/ui/combobox'
import { Toggle } from '@/components/ui/toggle'
import type { OrgHangarShipCard } from '../schemas/hangarShipCard'

const SORT_OPTIONS = [
  { value: 'ownerCount', label: 'Most Owners' },
  { value: 'name', label: 'Ship Name' },
]

export function OrgHangarView() {
  const [search, setSearch] = useState('')
  const [mine, setMine] = useState(false)
  const [memberId, setMemberId] = useState<string | undefined>(undefined)
  const [sortBy, setSortBy] = useState<'ownerCount' | 'name'>('ownerCount')

  const { data, isLoading, fetchNextPage, hasNextPage } = useOrgHangar(search || undefined, mine, memberId, sortBy)
  const { data: members } = useOwningMembers()

  const ships = data?.pages.flatMap((p) => p.items) ?? []

  const memberOptions = (members ?? []).map((m) => ({ value: m.userId, label: m.displayName }))

  const handleMemberChange = (id: string) => {
    setMemberId(id || undefined)
    if (id) setMine(false)
  }

  const handleMineToggle = () => {
    if (!mine) setMemberId(undefined)
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
            <Toggle
              pressed={mine}
              onPressedChange={handleMineToggle}
              variant="outline"
              size="sm"
              className="rounded-full text-xs"
              aria-label="Show my ships only"
            >
              My Ships
            </Toggle>

            <Combobox
              options={memberOptions}
              value={memberId ?? ''}
              onValueChange={handleMemberChange}
              placeholder="All Members"
              searchPlaceholder="Search members…"
              className="h-8 w-36 text-xs"
              aria-label="Filter by member"
            />

            <Combobox
              options={SORT_OPTIONS}
              value={sortBy}
              onValueChange={(v) => { if (v) setSortBy(v as 'ownerCount' | 'name') }}
              placeholder="Sort by…"
              searchPlaceholder="Search…"
              className="h-8 w-36 text-xs"
              aria-label="Sort by"
            />
          </div>
        }
      />
    </div>
  )
}
