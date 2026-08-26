// The console's signature element: a live ping standing in for a device
// checking in. Reused everywhere a device's live state needs a face —
// slow green rings while protected, still grey when protection is off,
// fast red rings while an alarm is actually sounding on the phone.
export default function Sonar({ state = 'off' }) {
  return (
    <div className={`sonar ${state}`} aria-hidden="true">
      <span className="sonar-ring" />
      <span className="sonar-ring" />
      <span className="sonar-ring" />
      <span className="sonar-core" />
    </div>
  )
}
