import { useSyncExternalStore } from 'react'

const STORAGE_KEY = 'student_id'

function readFromStorage(): string {
  try {
    return sessionStorage.getItem(STORAGE_KEY) ?? ''
  } catch {
    return ''
  }
}

function writeToStorage(id: string) {
  try {
    if (id) sessionStorage.setItem(STORAGE_KEY, id)
    else sessionStorage.removeItem(STORAGE_KEY)
  } catch {
    // storage unavailable (private mode etc.) – keep in-memory value only
  }
}

// Module-level state so every component shares the same value without a Context.
let _studentId = readFromStorage()
const _listeners = new Set<() => void>()

function subscribe(listener: () => void): () => void {
  _listeners.add(listener)
  return () => {
    _listeners.delete(listener)
  }
}

function getSnapshot(): string {
  return _studentId
}

export function setGlobalStudentId(id: string) {
  if (id === _studentId) return
  _studentId = id
  writeToStorage(id)
  _listeners.forEach((fn) => fn())
}

export function useStudentId() {
  // useSyncExternalStore handles subscribe/unsubscribe and avoids tearing.
  const studentId = useSyncExternalStore(subscribe, getSnapshot, getSnapshot)

  return {
    studentId,
    setStudentId: setGlobalStudentId,
    isValid: /^\d{17}$/.test(studentId),
  }
}
