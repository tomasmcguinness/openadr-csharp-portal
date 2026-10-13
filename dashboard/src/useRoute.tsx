import { useCallback, useEffect, useState, type ReactNode } from 'react'

/** Minimal client-side router: tracks window.location.pathname, updates it via the History
 * API, and re-renders on back/forward. Deliberately hand-rolled rather than pulling in a
 * routing library for two in-app pages. */
export function useRoute() {
  const [path, setPath] = useState(window.location.pathname)

  useEffect(() => {
    const onPopState = () => setPath(window.location.pathname)
    window.addEventListener('popstate', onPopState)
    return () => window.removeEventListener('popstate', onPopState)
  }, [])

  const navigate = useCallback((to: string) => {
    window.history.pushState({}, '', to)
    setPath(to)
  }, [])

  return { path, navigate }
}

/** An internal link that navigates via useRoute instead of a full page load, while still
 * rendering a real <a href> (middle-click / open-in-new-tab / right-click-copy-link all keep
 * working, and it degrades to a normal link if JS hasn't hydrated yet). */
export function NavLink({ to, navigate, className, children }: {
  to: string
  navigate: (to: string) => void
  className?: string
  children: ReactNode
}) {
  return (
    <a
      href={to}
      className={className}
      onClick={(e) => {
        if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return
        e.preventDefault()
        navigate(to)
      }}
    >
      {children}
    </a>
  )
}
