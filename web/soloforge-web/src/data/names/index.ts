import { names as fantasy } from './fantasy'
import { names as medieval } from './medieval'
import { names as tavern } from './tavern'
import { names as placeFantasy } from './place-fantasy'

export interface NameStyle {
  id: string
  name: string
  description: string
  names: string[]
}

export const NAME_STYLES: NameStyle[] = [
  { id: 'fantasy', name: 'Fantasy', description: 'Classic fantasy character names', names: fantasy },
  { id: 'medieval', name: 'Medieval', description: 'Historical medieval names', names: medieval },
  { id: 'tavern', name: 'Tavern / Inn', description: 'Tavern and inn names', names: tavern },
  { id: 'place-fantasy', name: 'Fantasy Places', description: 'Fantasy location and settlement names', names: placeFantasy },
]
