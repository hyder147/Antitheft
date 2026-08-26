export default function Logo({ size = 30 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg">
      <path
        d="M16 3L27 7V15C27 22.5 22.4 27.7 16 29.5C9.6 27.7 5 22.5 5 15V7L16 3Z"
        stroke="#4CFFB0"
        strokeWidth="1.6"
        strokeLinejoin="round"
        fill="rgba(76,255,176,0.06)"
      />
      <circle cx="16" cy="16" r="2.1" fill="#4CFFB0" />
      <path d="M16 16 L16 9.5" stroke="#4CFFB0" strokeWidth="1.4" strokeLinecap="round" opacity="0.85" />
      <path
        d="M16 10 A6 6 0 0 1 22 16"
        stroke="#4CFFB0"
        strokeWidth="1.2"
        strokeLinecap="round"
        opacity="0.45"
      />
    </svg>
  )
}
