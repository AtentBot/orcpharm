// Formula Clear Design System
// Pharmaceutical precision — emerald teal + warm amber accents

export const COLORS = {
  primary: '#0D9488',
  primaryDark: '#0F766E',
  primaryLight: '#99F6E4',
  primaryMuted: 'rgba(13, 148, 136, 0.12)',

  accent: '#F59E0B',
  accentDark: '#D97706',
  accentLight: '#FEF3C7',

  purple: '#7C3AED',

  success: '#059669',
  successLight: '#D1FAE5',
  warning: '#EA580C',
  warningLight: '#FFF7ED',
  error: '#DC2626',
  errorLight: '#FEE2E2',

  text: '#1C1917',
  textSecondary: '#57534E',
  textMuted: '#A8A29E',

  white: '#FFFFFF',
  background: '#FAFAF9',
  backgroundLight: '#F5F5F4',
  cardGlass: 'rgba(255, 255, 255, 0.92)',

  border: '#E7E5E4',
  borderLight: '#F5F5F4',
  borderFocus: '#0D9488',
};

export const GRADIENTS = {
  background: ['#FAFAF9', '#F5F5F4', '#FAFAF9'],
  primary: ['#0D9488', '#0F766E'],
  primarySoft: ['#CCFBF1', '#99F6E4'],
  purple: ['#7C3AED', '#6D28D9'],
  accent: ['#F59E0B', '#D97706'],
  success: ['#059669', '#047857'],
  promo: ['#134E4A', '#0F766E'],
  dark: ['#292524', '#1C1917'],
  darkTeal: ['#134E4A', '#0F766E'],
  orderAvatar: ['#0D9488', '#059669'],
  splash: ['#0D9488', '#134E4A', '#0F766E'],
};

export const SPACING = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 24,
  xxxl: 32,
  xxxxl: 48,
};

export const BORDER_RADIUS = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 24,
  full: 100,
};

export const FONT_SIZES = {
  xxs: 10,
  xs: 11,
  sm: 13,
  md: 15,
  lg: 17,
  xl: 20,
  xxl: 24,
  xxxl: 32,
  logo: 28,
  hero: 36,
};

export const SHADOWS = {
  small: {
    shadowColor: '#78716C',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06,
    shadowRadius: 3,
    elevation: 2,
  },
  medium: {
    shadowColor: '#78716C',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 12,
    elevation: 4,
  },
  large: {
    shadowColor: '#78716C',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.15,
    shadowRadius: 24,
    elevation: 8,
  },
  colored: (color) => ({
    shadowColor: color,
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.35,
    shadowRadius: 12,
    elevation: 8,
  }),
  glow: (color) => ({
    shadowColor: color,
    shadowOffset: { width: 0, height: 0 },
    shadowOpacity: 0.3,
    shadowRadius: 16,
    elevation: 6,
  }),
};

export default {
  COLORS,
  GRADIENTS,
  SPACING,
  BORDER_RADIUS,
  FONT_SIZES,
  SHADOWS,
};
