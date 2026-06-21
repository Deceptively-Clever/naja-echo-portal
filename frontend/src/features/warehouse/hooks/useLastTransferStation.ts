import { useState } from 'react'
import type { StationOption } from '../schemas/stationSchemas'

let _lastStation: StationOption | undefined = undefined

export function useLastTransferStation() {
  const [, forceUpdate] = useState(0)

  const setLastStation = (station: StationOption) => {
    _lastStation = station
    forceUpdate((n) => n + 1)
  }

  return { lastStation: _lastStation, setLastStation }
}
