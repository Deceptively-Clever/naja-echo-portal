import { useRef, useState } from 'react'
import { X, Upload, AlertTriangle } from 'lucide-react'
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

  if (!open) return null

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
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
      role="dialog"
      aria-modal="true"
      aria-label="Import Hangar"
    >
      <div className="relative bg-background border border-border rounded-lg shadow-xl w-full max-w-md mx-4">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-border">
          <h2 className="text-base font-semibold">Import Hangar</h2>
          <button onClick={handleClose} aria-label="Close dialog" className="text-muted-foreground hover:text-foreground">
            <X className="h-4 w-4" aria-hidden />
          </button>
        </div>

        {/* Body */}
        <div className="p-4">
          {step === 'warning' && (
            <div className="flex flex-col gap-4">
              <div className="flex gap-3 p-3 rounded-md bg-amber-50 dark:bg-amber-900/20 text-amber-800 dark:text-amber-300 border border-amber-200 dark:border-amber-800">
                <AlertTriangle className="h-5 w-5 shrink-0 mt-0.5" aria-hidden />
                <p className="text-sm">
                  Importing will <strong>replace your entire hangar</strong>. All existing ships will be
                  removed and replaced with ships from the imported file. This cannot be undone.
                </p>
              </div>
              <div className="flex justify-end gap-2">
                <button
                  onClick={handleClose}
                  className="px-3 py-1.5 text-sm rounded-md border border-border hover:bg-muted/50"
                >
                  Cancel
                </button>
                <button
                  onClick={handleProceed}
                  className="px-3 py-1.5 text-sm rounded-md bg-primary text-primary-foreground hover:bg-primary/90"
                >
                  I Understand, Continue
                </button>
              </div>
            </div>
          )}

          {step === 'file' && (
            <div className="flex flex-col gap-4">
              <p className="text-sm text-muted-foreground">
                Select your HangarXPLOR JSON export file.
              </p>

              {error && (
                <div role="alert" className="p-2 rounded text-sm bg-destructive/10 text-destructive">
                  {error}
                </div>
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

              <div className="flex justify-end">
                <button
                  onClick={handleClose}
                  disabled={isPending}
                  className="px-3 py-1.5 text-sm rounded-md border border-border hover:bg-muted/50 disabled:opacity-50"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}

          {step === 'summary' && result && (
            <div className="flex flex-col gap-4">
              <div className="grid grid-cols-2 gap-2 text-sm">
                <span className="text-muted-foreground">Total records</span>
                <span className="font-medium">{result.totalRecords}</span>
                <span className="text-muted-foreground">Ships imported</span>
                <span className="font-medium text-green-700 dark:text-green-400">{result.importedShips}</span>
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

              <div className="flex justify-end">
                <button
                  onClick={handleClose}
                  className="px-3 py-1.5 text-sm rounded-md bg-primary text-primary-foreground hover:bg-primary/90"
                >
                  Done
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
