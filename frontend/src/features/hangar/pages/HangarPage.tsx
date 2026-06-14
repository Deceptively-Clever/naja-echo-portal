import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { MyHangarView } from './MyHangarView'
import { OrgHangarView } from './OrgHangarView'

export function HangarPage() {
  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-bold">Hangar</h1>
        <p className="text-sm text-muted-foreground">Your ships and the org fleet</p>
      </div>

      <Tabs defaultValue="mine">
        <TabsList>
          <TabsTrigger value="mine">My Hangar</TabsTrigger>
          <TabsTrigger value="org">Org Hangar</TabsTrigger>
        </TabsList>
        <TabsContent value="mine">
          <MyHangarView />
        </TabsContent>
        <TabsContent value="org">
          <OrgHangarView />
        </TabsContent>
      </Tabs>
    </div>
  )
}
