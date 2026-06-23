import { useState } from 'react'
import type { LocationOption } from '../schemas/locationSchemas'

let _lastLocation: LocationOption | undefined = undefined

export function useLastTransferLocation() {
  const [, forceUpdate] = useState(0)

  const setLastLocation = (location: LocationOption) => {
    _lastLocation = location
    forceUpdate((n) => n + 1)
  }

  return { lastLocation: _lastLocation, setLastLocation }
}
