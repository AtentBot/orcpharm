import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import Svg, { Path, Rect } from 'react-native-svg';
import { COLORS, FONT_SIZES } from '../constants/theme';

/**
 * Farmify brand mark — cápsula-F glyph + texto "farmify." com ponto em âmbar
 * Réplica do SVG usado no web (wwwroot Views/Shared)
 */
const FarmifyLogo = ({
  size = 28,
  color = COLORS.primary,
  accent = COLORS.accent,
  textColor,
  inverted = false,
  showText = true,
  style,
}) => {
  const glyphColor = color;
  const finalTextColor = textColor || (inverted ? COLORS.white : COLORS.ink);

  return (
    <View style={[styles.row, style]}>
      <Svg width={size} height={size} viewBox="0 0 88 88">
        {/* "F" estilizada como cápsula farmacêutica */}
        <Path
          d="M18 18 H36 a16 16 0 0 1 16 16 V52 H36 a16 16 0 0 1 -16 -16 V18 Z"
          fill={glyphColor}
        />
        <Rect x="52" y="18" width="22" height="11" rx="5.5" fill={glyphColor} />
        <Rect x="52" y="35" width="16" height="10" rx="5" fill={glyphColor} />
      </Svg>
      {showText && (
        <Text style={[styles.brandName, { color: finalTextColor, marginLeft: size * 0.3 }]}>
          farmify
          <Text style={{ color: accent }}>.</Text>
        </Text>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  brandName: {
    fontSize: FONT_SIZES.xl,
    fontWeight: '700',
    letterSpacing: -0.5,
  },
});

export default FarmifyLogo;
