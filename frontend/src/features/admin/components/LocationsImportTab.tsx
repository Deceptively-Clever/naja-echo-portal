import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { StatusMessage } from '@/components/StatusMessage'
import { ApiError } from '@/lib/apiClient'
import { useImportLocations } from '../hooks/useImportLocations'
import type { ImportLocationsResponse } from '../schemas/locationSchemas'

export function LocationsImportTab() {
  const { mutate, isPending, isSuccess, isError, error, data } = useImportLocations()

  const message = (() => {
    if (!isSuccess && !isError) return null
    if (isError) {
      if (error instanceof ApiError && error.status === 409) {
        return { type: 'warning' as const, text: 'An import is already in progress. Please wait and try again.' }
      }
      if (error instanceof ApiError && error.status === 502) {
        return { type: 'error' as const, text: `Import failed: ${error.message}` }
      }
      return { type: 'error' as const, text: 'Import failed. Please try again.' }
    }
    if (data) {
      return { type: 'success' as const, text: 'Import completed successfully.' }
    }
    return null
  })()

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold">Star Systems & Space Stations</h2>
          <p className="text-sm text-muted-foreground">Import the complete star systems and space stations catalog from UEX Corp.</p>
        </div>
        <Button onClick={() => mutate()} disabled={isPending} aria-busy={isPending}>
          {isPending ? 'Importing…' : 'Import Locations'}
        </Button>
      </div>

      {data && <ImportSummary data={data} />}
      {message && <StatusMessage type={message.type}>{message.text}</StatusMessage>}
    </div>
  )
}

function ImportSummary({ data }: { data: ImportLocationsResponse }) {
  return (
    <div className="grid gap-4 md:grid-cols-2">
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Star Systems</CardTitle>
          <CardDescription>Import results for star systems</CardDescription>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Added:</span>
            <span className="font-medium">{data.starSystems.added}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Updated:</span>
            <span className="font-medium">{data.starSystems.updated}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Reactivated:</span>
            <span className="font-medium">{data.starSystems.reactivated}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Soft Deleted:</span>
            <span className="font-medium">{data.starSystems.softDeleted}</span>
          </div>
          <div className="border-t pt-2 flex justify-between font-semibold">
            <span>Total:</span>
            <span>{data.starSystems.total}</span>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Space Stations</CardTitle>
          <CardDescription>Import results for space stations</CardDescription>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Added:</span>
            <span className="font-medium">{data.spaceStations.added}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Updated:</span>
            <span className="font-medium">{data.spaceStations.updated}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Reactivated:</span>
            <span className="font-medium">{data.spaceStations.reactivated}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Soft Deleted:</span>
            <span className="font-medium">{data.spaceStations.softDeleted}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Skipped:</span>
            <span className="font-medium">{data.spaceStations.skipped}</span>
          </div>
          <div className="border-t pt-2 flex justify-between font-semibold">
            <span>Total:</span>
            <span>{data.spaceStations.total}</span>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
