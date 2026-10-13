interface LinkItem {
  label: string
  href: string
  description: string
}

const links: LinkItem[] = [
  {
    label: 'Source on GitHub',
    href: 'https://github.com/tomasmcguinness/openadr-csharp-portal',
    description: 'This VTN, the fake VEN, and this dashboard — all of it.',
  },
  {
    label: 'YouTube',
    href: 'https://www.youtube.com/tomasmcguinness',
    description: 'Videos on this project and other embedded/IoT builds.',
  },
  {
    label: 'Blog',
    href: 'https://tomasmcguinness.com',
    description: 'Longer write-ups.',
  },
  {
    label: 'Bluesky',
    href: 'https://bsky.app/profile/tomasmcguinness.bsky.social',
    description: '@tomasmcguinness.bsky.social',
  },
]

export function AboutPage() {
  return (
    <>
      <header className="page-header">
        <h1>About</h1>
        <p className="page-subtitle">What this is, and where to find more</p>
      </header>

      <div className="about-content">
        <p>
          This is a minimal <strong>OpenADR 3.1 VTN</strong> (Virtual Top Node), built as a demo
          cloud backend for an ESP32-based Home Energy Manager. The HEMS acts as an OpenADR VEN
          and sends an hourly whole-home demand forecast up to this VTN over REST, with live
          notifications pushed back down over MQTT — the flow this dashboard is showing you,
          live, as reports come in.
        </p>
        <p>
          It's a demo, not a production system: no TLS, no MQTT auth, no access control. The
          interesting part is the OpenADR 3.1 protocol flow itself, and getting it running end
          to end against real(ish) hardware.
        </p>

        <ul className="link-list">
          {links.map((link) => (
            <li key={link.href}>
              <a href={link.href} target="_blank" rel="noreferrer">
                {link.label}
              </a>
              <span className="link-description">{link.description}</span>
            </li>
          ))}
        </ul>
      </div>
    </>
  )
}
