import { useState } from 'react'
import { MyHangarView } from './MyHangarView'
import { OrgHangarView } from './OrgHangarView'

type Tab = 'mine' | 'org'

export function HangarPage() {
  const [tab, setTab] = useState<Tab>('mine')

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-bold">Hangar</h1>
        <p className="text-sm text-muted-foreground">Your ships and the org fleet</p>
      </div>

      {/* Sub-navigation tabs */}
      <div className="flex gap-1 border-b border-border">
        <button
          onClick={() => setTab('mine')}
          className={`px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'mine'
              ? 'border-b-2 border-primary text-primary'
              : 'text-muted-foreground hover:text-foreground'
          }`}
          aria-current={tab === 'mine' ? 'page' : undefined}
        >
          My Hangar
        </button>
        <button
          onClick={() => setTab('org')}
          className={`px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'org'
              ? 'border-b-2 border-primary text-primary'
              : 'text-muted-foreground hover:text-foreground'
          }`}
          aria-current={tab === 'org' ? 'page' : undefined}
        >
          Org Hangar
        </button>
      </div>

      {tab === 'mine' ? <MyHangarView /> : <OrgHangarView />}
    </div>
  )
}
