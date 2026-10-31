<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch, nextTick } from 'vue'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import {
  Search,
  MapPin,
  Navigation,
  Loader2,
  Check,
  AlertCircle,
  X,
} from 'lucide-vue-next'

export interface LocationDetails {
  latitude: number | null
  longitude: number | null
  addressLine1: string
  addressLine2?: string
  city: string
  stateProvince: string
  postalCode: string
  countryCode: string
  displayName?: string
}

const props = withDefaults(
  defineProps<{
    latitude?: number | null
    longitude?: number | null
    addressLine1?: string
    addressLine2?: string
    city?: string
    stateProvince?: string
    postalCode?: string
    countryCode?: string
    disabled?: boolean
  }>(),
  {
    latitude: null,
    longitude: null,
    addressLine1: '',
    addressLine2: '',
    city: '',
    stateProvince: '',
    postalCode: '',
    countryCode: '',
    disabled: false,
  }
)

const emit = defineEmits<{
  (e: 'update:latitude', val: number | null): void
  (e: 'update:longitude', val: number | null): void
  (e: 'update:addressLine1', val: string): void
  (e: 'update:addressLine2', val: string): void
  (e: 'update:city', val: string): void
  (e: 'update:stateProvince', val: string): void
  (e: 'update:postalCode', val: string): void
  (e: 'update:countryCode', val: string): void
  (e: 'change', details: LocationDetails): void
}>()

const mapContainer = ref<HTMLElement | null>(null)
let map: L.Map | null = null
let marker: L.Marker | null = null

// Search state
const searchQuery = ref('')
const searchLoading = ref(false)
const searchResults = ref<any[]>([])
const showSearchResults = ref(false)

// Geocoding & GPS status
const geocodingLoading = ref(false)
const gpsLoading = ref(false)
const statusMessage = ref<string>('')
const statusType = ref<'success' | 'error' | 'info' | ''>('')

// Custom brand-styled pin
const customPinIcon = L.divIcon({
  className: 'rp-map-pin',
  html: `
    <div style="
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #4f46e5;
      border: 3px solid #ffffff;
      border-radius: 50% 50% 50% 0;
      transform: rotate(-45deg);
      box-shadow: 0 4px 10px rgba(0,0,0,0.35);
      cursor: pointer;
    ">
      <div style="
        width: 10px;
        height: 10px;
        background: #ffffff;
        border-radius: 50%;
        transform: rotate(45deg);
      "></div>
    </div>
  `,
  iconSize: [32, 32],
  iconAnchor: [16, 32],
  popupAnchor: [0, -32],
})

onMounted(() => {
  nextTick(() => {
    initMap()
  })
})

onBeforeUnmount(() => {
  if (map) {
    map.remove()
    map = null
  }
})

function initMap() {
  if (!mapContainer.value) return

  // Default coordinate: fallback to [7.8731, 80.7718] (or [20, 0])
  const initialLat = props.latitude ?? 6.9271
  const initialLng = props.longitude ?? 79.8612
  const initialZoom = props.latitude && props.longitude ? 15 : 4

  map = L.map(mapContainer.value, {
    zoomControl: false,
    attributionControl: false,
  }).setView([initialLat, initialLng], initialZoom)

  L.control.zoom({ position: 'bottomright' }).addTo(map)

  // Standard OpenStreetMap tiles
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
  }).addTo(map)

  // Place initial marker if coordinates provided
  if (props.latitude != null && props.longitude != null) {
    placeMarker(props.latitude, props.longitude, false)
  }

  // Click on map to place pin
  map.on('click', (e: L.LeafletMouseEvent) => {
    if (props.disabled) return
    const { lat, lng } = e.latlng
    handleCoordinateSelection(lat, lng)
  })

  // Ensure tiles render completely after layout
  setTimeout(() => {
    map?.invalidateSize()
  }, 200)
}

function placeMarker(lat: number, lng: number, fly = true) {
  if (!map) return

  if (marker) {
    marker.setLatLng([lat, lng])
  } else {
    marker = L.marker([lat, lng], {
      icon: customPinIcon,
      draggable: !props.disabled,
    }).addTo(map)

    marker.on('dragend', (e) => {
      const pos = e.target.getLatLng()
      handleCoordinateSelection(pos.lat, pos.lng)
    })
  }

  if (fly) {
    map.flyTo([lat, lng], Math.max(map.getZoom(), 15), {
      duration: 1.2,
    })
  }
}

async function handleCoordinateSelection(lat: number, lng: number) {
  // Round to 7 decimal places for precision (~1cm)
  const roundedLat = parseFloat(lat.toFixed(7))
  const roundedLng = parseFloat(lng.toFixed(7))

  placeMarker(roundedLat, roundedLng, false)

  emit('update:latitude', roundedLat)
  emit('update:longitude', roundedLng)

  // Trigger reverse geocoding to resolve address fields
  await reverseGeocode(roundedLat, roundedLng)
}

async function reverseGeocode(lat: number, lng: number) {
  geocodingLoading.value = true
  statusMessage.value = 'Resolving address from map...'
  statusType.value = 'info'

  try {
    const url = `https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${lat}&lon=${lng}&addressdetails=1`
    const res = await fetch(url, {
      headers: {
        'Accept-Language': 'en',
      },
    })

    if (!res.ok) throw new Error('Could not fetch address details')

    const data = await res.json()
    const addr = data.address || {}

    // Extract address components
    const countryCode = (addr.country_code || '').toUpperCase()
    const city =
      addr.city ||
      addr.town ||
      addr.village ||
      addr.municipality ||
      addr.suburb ||
      addr.county ||
      ''
    const stateProvince = addr.state || addr.province || addr.region || ''
    const postalCode = addr.postcode || ''

    // Road / Street
    const street = addr.road || addr.pedestrian || addr.street || addr.neighbourhood || ''
    const houseNumber = addr.house_number || ''
    const addressLine1 = [houseNumber, street].filter(Boolean).join(' ') || data.name || ''

    // Emit updates
    emit('update:countryCode', countryCode)
    emit('update:city', city)
    emit('update:stateProvince', stateProvince)
    emit('update:postalCode', postalCode)
    if (addressLine1) {
      emit('update:addressLine1', addressLine1)
    }

    emit('change', {
      latitude: lat,
      longitude: lng,
      addressLine1: addressLine1 || props.addressLine1 || '',
      addressLine2: props.addressLine2,
      city,
      stateProvince,
      postalCode,
      countryCode,
      displayName: data.display_name,
    })

    statusMessage.value = data.display_name || 'Location selected'
    statusType.value = 'success'
  } catch (err: any) {
    console.warn('Reverse geocode error:', err)
    statusMessage.value = 'Location pinned (could not auto-resolve address details)'
    statusType.value = 'info'
  } finally {
    geocodingLoading.value = false
  }
}

// Search address (Forward Geocoding)
let searchTimeout: any = null
function onSearchInput() {
  clearTimeout(searchTimeout)
  if (!searchQuery.value.trim() || searchQuery.value.trim().length < 3) {
    searchResults.value = []
    showSearchResults.value = false
    return
  }

  searchTimeout = setTimeout(() => {
    performSearch()
  }, 400)
}

async function performSearch() {
  const q = searchQuery.value.trim()
  if (!q) return

  searchLoading.value = true
  try {
    const url = `https://nominatim.openstreetmap.org/search?format=jsonv2&q=${encodeURIComponent(
      q
    )}&limit=5&addressdetails=1`
    const res = await fetch(url, {
      headers: {
        'Accept-Language': 'en',
      },
    })
    if (!res.ok) throw new Error('Search failed')

    const data = await res.json()
    searchResults.value = data || []
    showSearchResults.value = searchResults.value.length > 0
  } catch (err) {
    console.warn('Location search error:', err)
    searchResults.value = []
  } finally {
    searchLoading.value = false
  }
}

function selectSearchResult(result: any) {
  const lat = parseFloat(result.lat)
  const lng = parseFloat(result.lon)

  searchQuery.value = result.display_name
  showSearchResults.value = false

  const addr = result.address || {}
  const countryCode = (addr.country_code || '').toUpperCase()
  const city =
    addr.city ||
    addr.town ||
    addr.village ||
    addr.municipality ||
    addr.suburb ||
    addr.county ||
    ''
  const stateProvince = addr.state || addr.province || addr.region || ''
  const postalCode = addr.postcode || ''
  const street = addr.road || addr.pedestrian || addr.street || addr.neighbourhood || ''
  const houseNumber = addr.house_number || ''
  const addressLine1 = [houseNumber, street].filter(Boolean).join(' ') || result.name || ''

  placeMarker(lat, lng, true)

  emit('update:latitude', lat)
  emit('update:longitude', lng)
  emit('update:countryCode', countryCode)
  emit('update:city', city)
  emit('update:stateProvince', stateProvince)
  emit('update:postalCode', postalCode)
  if (addressLine1) {
    emit('update:addressLine1', addressLine1)
  }

  emit('change', {
    latitude: lat,
    longitude: lng,
    addressLine1: addressLine1 || props.addressLine1 || '',
    addressLine2: props.addressLine2,
    city,
    stateProvince,
    postalCode,
    countryCode,
    displayName: result.display_name,
  })

  statusMessage.value = result.display_name
  statusType.value = 'success'
}

// GPS Current Location
function locateMe() {
  if (!navigator.geolocation) {
    statusMessage.value = 'Geolocation is not supported by your browser'
    statusType.value = 'error'
    return
  }

  gpsLoading.value = true
  statusMessage.value = 'Locating your current position...'
  statusType.value = 'info'

  navigator.geolocation.getCurrentPosition(
    (pos) => {
      gpsLoading.value = false
      const lat = pos.coords.latitude
      const lng = pos.coords.longitude
      map?.flyTo([lat, lng], 16)
      handleCoordinateSelection(lat, lng)
    },
    (err) => {
      gpsLoading.value = false
      statusMessage.value = err.message || 'Unable to retrieve your location'
      statusType.value = 'error'
    },
    { enableHighAccuracy: true, timeout: 10000 }
  )
}

// External coordinate sync
watch(
  () => [props.latitude, props.longitude],
  ([newLat, newLng]) => {
    if (newLat != null && newLng != null && map) {
      if (
        !marker ||
        marker.getLatLng().lat !== newLat ||
        marker.getLatLng().lng !== newLng
      ) {
        placeMarker(newLat, newLng, false)
      }
    }
  }
)
</script>

<template>
  <div class="space-y-3">
    <!-- Map Controls & Search Bar -->
    <div class="relative z-20 flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
      <!-- Search Input -->
      <div class="relative flex-1">
        <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-subtle" />
        <input
          v-model="searchQuery"
          type="text"
          :disabled="disabled"
          placeholder="Search place, city, or address..."
          @input="onSearchInput"
          @focus="showSearchResults = searchResults.length > 0"
          @keydown.enter.prevent="performSearch"
          class="w-full pl-9 pr-9 py-2 text-xs rounded-xl border border-border bg-card text-text-strong shadow-2xs focus:ring-1 focus:ring-primary focus:border-primary placeholder:text-text-subtle"
        />
        <button
          v-if="searchQuery"
          type="button"
          @click="
            searchQuery = '';
            searchResults = [];
            showSearchResults = false
          "
          class="absolute right-3 top-1/2 -translate-y-1/2 text-text-subtle hover:text-text-main"
        >
          <X class="h-3.5 w-3.5" />
        </button>

        <!-- Search Results Dropdown -->
        <div
          v-if="showSearchResults && searchResults.length > 0"
          class="absolute left-0 right-0 mt-1 max-h-56 overflow-y-auto rounded-xl bg-card border border-border shadow-lg py-1 z-50 text-xs divide-y divide-border"
        >
          <button
            v-for="item in searchResults"
            :key="item.place_id"
            type="button"
            @click="selectSearchResult(item)"
            class="w-full text-left px-3.5 py-2.5 hover:bg-surface-raised flex items-start gap-2.5 transition text-text-main"
          >
            <MapPin class="h-3.5 w-3.5 text-primary shrink-0 mt-0.5" />
            <div class="min-w-0 flex-1">
              <p class="font-medium truncate text-text-strong">
                {{ item.name || item.display_name.split(',')[0] }}
              </p>
              <p class="text-[11px] text-text-muted truncate">
                {{ item.display_name }}
              </p>
            </div>
          </button>
        </div>
      </div>

      <!-- Locate Me GPS Button -->
      <button
        type="button"
        @click="locateMe"
        :disabled="disabled || gpsLoading"
        title="Use my current location"
        class="inline-flex items-center justify-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold bg-surface-raised hover:bg-surface border border-border text-text-secondary transition shrink-0 shadow-2xs disabled:opacity-50"
      >
        <Loader2 v-if="gpsLoading" class="h-3.5 w-3.5 animate-spin text-primary" />
        <Navigation v-else class="h-3.5 w-3.5 text-primary" />
        <span>Locate Me</span>
      </button>
    </div>

    <!-- Map Canvas Container -->
    <div class="relative rounded-2xl overflow-hidden border border-border shadow-xs">
      <div
        ref="mapContainer"
        class="w-full h-72 sm:h-80 z-10 outline-none"
        style="min-height: 280px;"
      ></div>

      <!-- Hint badge overlay -->
      <div
        class="absolute top-3 left-3 z-20 pointer-events-none bg-card/90 backdrop-blur-xs px-2.5 py-1 rounded-lg border border-border shadow-xs text-[11px] font-medium text-text-secondary flex items-center gap-1.5"
      >
        <MapPin class="h-3.5 w-3.5 text-primary" />
        <span>Click or drag pin to select exact office location</span>
      </div>

      <!-- Loading geocode indicator -->
      <div
        v-if="geocodingLoading"
        class="absolute inset-0 z-30 bg-card/50 backdrop-blur-2xs flex items-center justify-center gap-2 text-xs font-semibold text-primary"
      >
        <Loader2 class="h-4 w-4 animate-spin" />
        <span>Fetching address details from map...</span>
      </div>
    </div>

    <!-- Status message / feedback -->
    <div
      v-if="statusMessage"
      class="text-[11px] flex items-center gap-1.5 px-1 text-text-muted truncate"
      :class="{
        'text-success font-medium': statusType === 'success',
        'text-danger font-medium': statusType === 'error',
      }"
    >
      <Check v-if="statusType === 'success'" class="h-3.5 w-3.5 text-success shrink-0" />
      <AlertCircle v-else-if="statusType === 'error'" class="h-3.5 w-3.5 text-danger shrink-0" />
      <MapPin v-else class="h-3.5 w-3.5 text-primary shrink-0" />
      <span class="truncate">{{ statusMessage }}</span>
    </div>
  </div>
</template>

<style>
/* Leaflet map base styling */
.leaflet-container {
  font-family: inherit;
  background-color: var(--bg-subtle, #f3f4f6);
}

/* Dark mode tile adaptation */
.dark .leaflet-tile {
  filter: brightness(0.75) invert(1) contrast(1.2) hue-rotate(200deg) saturate(0.35);
}

.dark .leaflet-container {
  background-color: var(--bg-card, #131b2e) !important;
}

/* Custom Pin Marker animation */
.rp-map-pin {
  transition: transform 0.15s ease-out;
}
.rp-map-pin:hover {
  transform: scale(1.1);
}
</style>

