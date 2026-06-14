import { useState } from 'react'
import { useOrgHangar } from '../hooks/useOrgHangar'
import { useOwningMembers } from '../hooks/useOwningMembers'
import { ShipCardGallery } from '../components/ShipCardGallery'
import { OwnerCountBadge } from '../components/OwnerCountBadge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Toggle } from '@/components/ui/toggle'
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

            {/* Member filter */}
            <Select
              value={memberId ?? ''}
              onValueChange={(v) => handleMemberChange(v === '__all__' ? '' : v)}
            >
              <SelectTrigger aria-label="Filter by member" className="h-8 w-36 text-xs">
                <SelectValue placeholder="All Members" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__all__">All Members</SelectItem>
                {(members ?? []).map((m) => (
                  <SelectItem key={m.userId} value={m.userId}>
                    {m.displayName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            {/* Sort control */}
            <Select value={sortBy} onValueChange={(v) => setSortBy(v as 'ownerCount' | 'name')}>
              <SelectTrigger aria-label="Sort by" className="h-8 w-36 text-xs">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="ownerCount">Most Owners</SelectItem>
                <SelectItem value="name">Ship Name</SelectItem>
              </SelectContent>
            </Select>
          </div>
        }
      />
    </div>
  )
}
