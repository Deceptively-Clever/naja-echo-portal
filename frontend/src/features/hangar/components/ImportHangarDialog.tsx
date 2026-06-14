import { useRef, useState } from 'react'
import { Upload, AlertTriangle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Dialog, DialogContent, DialogHeader, DialogFooter, DialogTitle } from '@/components/ui/dialog'
import { useImportHangar } from '../hooks/useImportHangar'
import { importShipRecordSchema, type ImportHangarResult } from '../schemas/hangarImport'
import { z } from 'zod'

const MAX_FILE_SIZE = 5 * 1024 * 1024 // 5 MB

interface ImportHangarDialogProps {
  open: boolean
  onClose: () => void
}

type Step = 'warning' | 'file' | 'summary'

export function ImportHangarDialog({ open, onClose }: ImportHangarDialogProps) {
  const [step, setStep] = useState<Step>('warning')
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<ImportHangarResult | null>(null)
  const fileRef = useRef<HTMLInputElement>(null)
  const { mutate: importHangar, isPending } = useImportHangar()

  function handleClose() {
    setStep('warning')
    setError(null)
    setResult(null)
    onClose()
  }

  function handleProceed() {
    setStep('file')
  }

  async function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setError(null)

    if (file.size > MAX_FILE_SIZE) {
      setError('File is too large. Maximum size is 5 MB.')
      return
    }

    let parsed: unknown
    try {
      const text = await file.text()
      parsed = JSON.parse(text)
    } catch {
      setError('Invalid JSON file. Please select a valid HangarXPLOR export.')
      return
    }

    const arrayResult = z.array(z.unknown()).safeParse(parsed)
    if (!arrayResult.success) {
      setError('Unexpected file format. Expected a JSON array.')
      return
    }

    const items = arrayResult.data.map((item) => {
      const r = importShipRecordSchema.safeParse(item)
      return r.success ? r.data : null
    }).filter((item): item is NonNullable<typeof item> => item !== null)

    if (items.length === 0 && arrayResult.data.length > 0) {
      setError('No valid ship records found in the file.')
      return
    }

    importHangar(items, {
      onSuccess: (data) => {
        setResult(data)
        setStep('summary')
      },
      onError: () => {
        setError('Import failed. Please try again.')
      },
    })
  }

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) handleClose() }}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Import Hangar</DialogTitle>
        </DialogHeader>

        {step === 'warning' && (
          <>
            <Alert variant="destructive">
              <AlertTriangle aria-hidden />
              <AlertDescription>
                Importing will <strong>replace your entire hangar</strong>. All existing ships will be
                removed and replaced with ships from the imported file. This cannot be undone.
              </AlertDescription>
            </Alert>
            <DialogFooter>
              <Button variant="outline" onClick={handleClose}>Cancel</Button>
              <Button onClick={handleProceed}>I Understand, Continue</Button>
            </DialogFooter>
          </>
        )}

        {step === 'file' && (
          <>
            <p className="text-sm text-muted-foreground">
              Select your HangarXPLOR JSON export file.
            </p>

            {error && (
              <Alert variant="destructive" role="alert">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <label className="flex flex-col items-center gap-2 p-6 border-2 border-dashed border-border rounded-lg cursor-pointer hover:bg-muted/30">
              <Upload className="h-6 w-6 text-muted-foreground" aria-hidden />
              <span className="text-sm font-medium">
                {isPending ? 'Importing…' : 'Click to select file'}
              </span>
              <span className="text-xs text-muted-foreground">JSON, max 5 MB</span>
              <input
                ref={fileRef}
                type="file"
                accept=".json,application/json"
                className="sr-only"
                disabled={isPending}
                onChange={handleFileChange}
                aria-label="Select HangarXPLOR JSON file"
              />
            </label>

            <DialogFooter>
              <Button variant="outline" onClick={handleClose} disabled={isPending}>Cancel</Button>
            </DialogFooter>
          </>
        )}

        {step === 'summary' && result && (
          <>
            <div className="grid grid-cols-2 gap-2 text-sm">
              <span className="text-muted-foreground">Total records</span>
              <span className="font-medium">{result.totalRecords}</span>
              <span className="text-muted-foreground">Ships imported</span>
              <span className="font-medium text-primary">{result.importedShips}</span>
              <span className="text-muted-foreground">Unmatched / skipped</span>
              <span className="font-medium">{result.unmatchedRecords}</span>
            </div>

            {result.unmatchedShipNames.length > 0 && (
              <div>
                <p className="text-xs font-medium text-muted-foreground mb-1">Unmatched ship names:</p>
                <ul className="text-xs text-muted-foreground list-disc list-inside max-h-32 overflow-y-auto">
                  {result.unmatchedShipNames.map((n) => (
                    <li key={n}>{n}</li>
                  ))}
                </ul>
              </div>
            )}

            <DialogFooter>
              <Button onClick={handleClose}>Done</Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
