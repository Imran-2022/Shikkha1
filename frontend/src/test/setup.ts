import '@testing-library/jest-dom'
import { vi } from 'vitest'

// Mock sessionStorage
const storageMock = (() => {
  let store: Record<string, string> = {}
  return {
    getItem: (key: string) => store[key] ?? null,
    setItem: (key: string, val: string) => { store[key] = val },
    removeItem: (key: string) => { delete store[key] },
    clear: () => { store = {} },
  }
})()

Object.defineProperty(window, 'sessionStorage', { value: storageMock })

// Silence act() warnings in tests
vi.spyOn(console, 'error').mockImplementation((msg: string) => {
  if (typeof msg === 'string' && msg.includes('act(')) return
  console.warn(msg)
})
