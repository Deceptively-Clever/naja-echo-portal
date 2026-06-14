import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { ShipsImportTab } from '../components/ShipsImportTab'
import { ItemsImportTab } from '../components/ItemsImportTab'
import { CommoditiesImportTab } from '../components/CommoditiesImportTab'

export function DataImportPage() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Data Import</h1>
        <p className="text-muted-foreground">Import game data from the UEX Corp feed.</p>
      </div>

      <Tabs defaultValue="ships">
        <TabsList>
          <TabsTrigger value="ships">Ships</TabsTrigger>
          <TabsTrigger value="items">Items</TabsTrigger>
          <TabsTrigger value="commodities">Commodities</TabsTrigger>
        </TabsList>
        <TabsContent value="ships">
          <ShipsImportTab />
        </TabsContent>
        <TabsContent value="items">
          <ItemsImportTab />
        </TabsContent>
        <TabsContent value="commodities">
          <CommoditiesImportTab />
        </TabsContent>
      </Tabs>
    </div>
  )
}
