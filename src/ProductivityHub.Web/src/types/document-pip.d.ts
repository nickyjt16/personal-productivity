// Minimal ambient types for the Document Picture-in-Picture API (Chrome/Edge 116+),
// which is not yet part of the standard TypeScript DOM lib.
interface DocumentPictureInPictureOptions {
  width?: number
  height?: number
}

interface DocumentPictureInPicture extends EventTarget {
  readonly window: Window | null
  requestWindow(options?: DocumentPictureInPictureOptions): Promise<Window>
}

interface Window {
  readonly documentPictureInPicture?: DocumentPictureInPicture
}
