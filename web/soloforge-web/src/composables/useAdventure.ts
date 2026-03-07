import { reactive } from 'vue'
import { apiSend } from './useApi'

export function useAdventure() {
  const loading = reactive({
    addCharacter: false,
    removeCharacter: false,
    addThread: false,
    closeThread: false,
    reopenThread: false,
  })
  async function addCharacter(name: string, description?: string | null): Promise<void> {
    loading.addCharacter = true
    try {
      await apiSend('/api/adventure/characters', 'POST', {
        name,
        description: description ?? null,
      })
    } finally {
      loading.addCharacter = false
    }
  }

  async function removeCharacter(name: string): Promise<void> {
    loading.removeCharacter = true
    try {
      await apiSend(`/api/adventure/characters?name=${encodeURIComponent(name)}`, 'DELETE')
    } finally {
      loading.removeCharacter = false
    }
  }

  async function addThread(name: string, description?: string | null): Promise<void> {
    loading.addThread = true
    try {
      await apiSend('/api/adventure/threads', 'POST', {
        name,
        description: description ?? null,
      })
    } finally {
      loading.addThread = false
    }
  }

  async function closeThread(name: string): Promise<void> {
    loading.closeThread = true
    try {
      await apiSend(`/api/adventure/threads/close?name=${encodeURIComponent(name)}`, 'POST')
    } finally {
      loading.closeThread = false
    }
  }

  async function reopenThread(name: string): Promise<void> {
    loading.reopenThread = true
    try {
      await apiSend(`/api/adventure/threads/reopen?name=${encodeURIComponent(name)}`, 'POST')
    } finally {
      loading.reopenThread = false
    }
  }

  return {
    loading,
    addCharacter,
    removeCharacter,
    addThread,
    closeThread,
    reopenThread,
  }
}
