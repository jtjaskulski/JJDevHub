/**
 * Shared visual tokens for JJDevHub web and future React Native.
 * No framework imports — plain values only.
 */

export const tokens = {
  color: {
    canvas: '#f5f5f7',
    white: '#ffffff',
    ink: '#1d1d1f',
    muted: '#6e6e73',
    accent: '#0071e3',
    darkChapter: {
      background: '#000000',
      foreground: '#f5f5f7',
    },
  },

  font: {
    family: 'Inter',
    size: {
      display: 80,
      h1: 56,
      h2: 40,
      h3: 28,
      h4: 21,
      body: 17,
      small: 14,
      caption: 12,
    },
    /** Letter-spacing in em, keyed like size. */
    tracking: {
      display: -0.015,
      h1: -0.012,
      h2: -0.01,
      h3: -0.008,
      h4: -0.005,
      body: -0.014,
      small: -0.006,
      caption: 0,
    },
  },

  space: {
    1: 4,
    2: 8,
    3: 12,
    4: 16,
    5: 24,
    6: 32,
    7: 48,
    8: 64,
    9: 80,
    10: 96,
    11: 128,
    12: 160,
  },

  radius: {
    sm: 8,
    md: 12,
    lg: 18,
    card: 18,
    pill: 9999,
  },

  /**
   * Milliseconds — reusable by CSS, GSAP, and RN Animated.
   */
  duration: {
    reveal: 700,
    revealFast: 400,
    heroPin: 1200,
    heroFade: 600,
    nav: 250,
  },

  /**
   * Cubic-bezier control points [x1, y1, x2, y2] for CSS and RN Animated.
   */
  easing: {
    reveal: [0.25, 0.1, 0.25, 1] as const,
    hero: [0.22, 1, 0.36, 1] as const,
    standard: [0.4, 0, 0.2, 1] as const,
  },
} as const;

export type Tokens = typeof tokens;

function px(n: number): string {
  return `${n}px`;
}

function em(n: number): string {
  return `${n}em`;
}

function ms(n: number): string {
  return `${n}ms`;
}

function cubicBezier(points: readonly [number, number, number, number]): string {
  return `cubic-bezier(${points.join(', ')})`;
}

/**
 * Flat map of CSS custom property names → values for documentElement.
 * Hex and scales live only in {@link tokens}; consumers read var(--…).
 */
export function cssCustomProperties(): Record<string, string> {
  const { color, font, space, radius, duration, easing } = tokens;

  return {
    '--canvas': color.canvas,
    '--white': color.white,
    '--ink': color.ink,
    '--muted': color.muted,
    '--accent': color.accent,
    '--dark-chapter-bg': color.darkChapter.background,
    '--dark-chapter-fg': color.darkChapter.foreground,

    '--font-family': font.family,
    '--font-display': px(font.size.display),
    '--font-h1': px(font.size.h1),
    '--font-h2': px(font.size.h2),
    '--font-h3': px(font.size.h3),
    '--font-h4': px(font.size.h4),
    '--font-body': px(font.size.body),
    '--font-small': px(font.size.small),
    '--font-caption': px(font.size.caption),
    '--tracking-display': em(font.tracking.display),
    '--tracking-h1': em(font.tracking.h1),
    '--tracking-h2': em(font.tracking.h2),
    '--tracking-h3': em(font.tracking.h3),
    '--tracking-h4': em(font.tracking.h4),
    '--tracking-body': em(font.tracking.body),
    '--tracking-small': em(font.tracking.small),
    '--tracking-caption': em(font.tracking.caption),

    '--space-1': px(space[1]),
    '--space-2': px(space[2]),
    '--space-3': px(space[3]),
    '--space-4': px(space[4]),
    '--space-5': px(space[5]),
    '--space-6': px(space[6]),
    '--space-7': px(space[7]),
    '--space-8': px(space[8]),
    '--space-9': px(space[9]),
    '--space-10': px(space[10]),
    '--space-11': px(space[11]),
    '--space-12': px(space[12]),

    '--radius-sm': px(radius.sm),
    '--radius-md': px(radius.md),
    '--radius-lg': px(radius.lg),
    '--radius-card': px(radius.card),
    '--radius-pill': px(radius.pill),

    '--duration-reveal': ms(duration.reveal),
    '--duration-reveal-fast': ms(duration.revealFast),
    '--duration-hero-pin': ms(duration.heroPin),
    '--duration-hero-fade': ms(duration.heroFade),
    '--duration-nav': ms(duration.nav),

    '--easing-reveal': cubicBezier(easing.reveal),
    '--easing-hero': cubicBezier(easing.hero),
    '--easing-standard': cubicBezier(easing.standard),
  };
}
