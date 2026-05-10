import React, { useState, useEffect } from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet, ScrollView, ActivityIndicator,
  StatusBar, RefreshControl,
} from 'react-native';
import { Feather } from '@expo/vector-icons';
import * as api from '../services/api';
import { formatCurrency } from '../utils/formatters';
import { COLORS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';
import FarmifyLogo from '../components/FarmifyLogo';

const CatalogScreen = ({ navigation }) => {
  const [productTypes, setProductTypes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [errorMsg, setErrorMsg] = useState(null);

  useEffect(() => {
    loadProductTypes();
  }, []);

  const loadProductTypes = async () => {
    try {
      setErrorMsg(null);
      const result = await api.getProductTypes();
      if (result?.success && Array.isArray(result.productTypes)) {
        setProductTypes(result.productTypes);
      } else if (Array.isArray(result?.data)) {
        setProductTypes(result.data);
      } else if (Array.isArray(result)) {
        setProductTypes(result);
      } else {
        setProductTypes([]);
      }
    } catch (err) {
      setErrorMsg('Não foi possível carregar o catálogo.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const onRefresh = () => {
    setRefreshing(true);
    loadProductTypes();
  };

  const iconForType = (name) => {
    const n = (name || '').toLowerCase();
    if (n.includes('capsul')) return 'circle';
    if (n.includes('creme') || n.includes('pomada')) return 'droplet';
    if (n.includes('solu') || n.includes('xarop')) return 'thermometer';
    if (n.includes('sache')) return 'package';
    if (n.includes('gel')) return 'cloud-drizzle';
    return 'box';
  };

  return (
    <View style={styles.container}>
      <StatusBar barStyle="dark-content" backgroundColor={COLORS.background} />

      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backBtn}>
          <Feather name="arrow-left" size={20} color={COLORS.ink} />
        </TouchableOpacity>
        <FarmifyLogo size={24} />
        <View style={{ width: 40 }} />
      </View>

      {/* Title */}
      <View style={styles.titleBlock}>
        <Text style={styles.title}>Catálogo</Text>
        <Text style={styles.subtitle}>Explore formas farmacêuticas disponíveis</Text>
      </View>

      {loading ? (
        <View style={styles.centered}>
          <ActivityIndicator size="large" color={COLORS.primary} />
        </View>
      ) : errorMsg ? (
        <View style={styles.centered}>
          <Feather name="alert-circle" size={32} color={COLORS.error} />
          <Text style={styles.errorText}>{errorMsg}</Text>
          <TouchableOpacity style={styles.retryBtn} onPress={loadProductTypes}>
            <Text style={styles.retryText}>Tentar novamente</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <ScrollView
          contentContainerStyle={styles.scrollContent}
          refreshControl={
            <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={COLORS.primary} />
          }
        >
          {productTypes.length === 0 ? (
            <View style={styles.emptyState}>
              <View style={styles.emptyIconWrap}>
                <Feather name="grid" size={28} color={COLORS.primary} />
              </View>
              <Text style={styles.emptyTitle}>Catálogo em construção</Text>
              <Text style={styles.emptyText}>
                Em breve você verá aqui produtos manipulados disponíveis pra
                comprar direto. Por enquanto, use "Receita" pra enviar a sua
                prescrição médica ou "Fórmula" pra montar uma personalizada.
              </Text>
              <TouchableOpacity
                style={styles.ctaBtn}
                onPress={() => navigation.navigate('Prescription')}
              >
                <Feather name="camera" size={18} color={COLORS.white} />
                <Text style={styles.ctaBtnText}>Enviar receita</Text>
              </TouchableOpacity>
            </View>
          ) : (
            <View style={styles.grid}>
              {productTypes.map((pt) => (
                <TouchableOpacity
                  key={pt.id || pt.name}
                  style={styles.card}
                  onPress={() => navigation.navigate('Formula', { productTypeId: pt.id })}
                  activeOpacity={0.85}
                >
                  <View style={styles.cardIconWrap}>
                    <Feather name={iconForType(pt.name)} size={24} color={COLORS.primary} />
                  </View>
                  <Text style={styles.cardTitle}>{pt.name}</Text>
                  {pt.description ? (
                    <Text style={styles.cardSub} numberOfLines={2}>{pt.description}</Text>
                  ) : null}
                  {pt.minimumPrice != null && pt.minimumPrice > 0 ? (
                    <Text style={styles.cardPrice}>a partir de {formatCurrency(pt.minimumPrice)}</Text>
                  ) : null}
                </TouchableOpacity>
              ))}
            </View>
          )}
        </ScrollView>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: COLORS.background },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: SPACING.lg,
    paddingTop: 56,
    paddingBottom: SPACING.lg,
  },
  backBtn: {
    width: 40, height: 40, borderRadius: BORDER_RADIUS.full,
    backgroundColor: COLORS.surface,
    alignItems: 'center', justifyContent: 'center',
    borderWidth: 1, borderColor: COLORS.border,
  },
  titleBlock: { paddingHorizontal: SPACING.lg, marginBottom: SPACING.lg },
  title: {
    fontSize: FONT_SIZES.xxl, fontWeight: '700', color: COLORS.ink,
    letterSpacing: -0.5,
  },
  subtitle: { fontSize: FONT_SIZES.md, color: COLORS.ink2, marginTop: 4 },
  scrollContent: { paddingHorizontal: SPACING.lg, paddingBottom: SPACING.xl },
  centered: {
    flex: 1, alignItems: 'center', justifyContent: 'center',
    paddingHorizontal: SPACING.xl,
  },
  errorText: { color: COLORS.error, marginTop: SPACING.md, textAlign: 'center' },
  retryBtn: {
    marginTop: SPACING.md, paddingVertical: SPACING.sm,
    paddingHorizontal: SPACING.lg, borderRadius: BORDER_RADIUS.md,
    borderWidth: 1, borderColor: COLORS.primary,
  },
  retryText: { color: COLORS.primary, fontWeight: '600' },
  emptyState: {
    backgroundColor: COLORS.surface,
    borderRadius: BORDER_RADIUS.lg,
    padding: SPACING.xl,
    alignItems: 'center',
    borderWidth: 1, borderColor: COLORS.border,
    ...SHADOWS.card,
  },
  emptyIconWrap: {
    width: 64, height: 64, borderRadius: BORDER_RADIUS.full,
    backgroundColor: COLORS.primarySoft,
    alignItems: 'center', justifyContent: 'center',
    marginBottom: SPACING.md,
  },
  emptyTitle: {
    fontSize: FONT_SIZES.lg, fontWeight: '700', color: COLORS.ink,
    marginBottom: SPACING.sm,
  },
  emptyText: {
    fontSize: FONT_SIZES.sm, color: COLORS.ink2,
    textAlign: 'center', lineHeight: 20, marginBottom: SPACING.lg,
  },
  ctaBtn: {
    flexDirection: 'row', alignItems: 'center', gap: SPACING.sm,
    backgroundColor: COLORS.primary,
    paddingVertical: SPACING.md, paddingHorizontal: SPACING.lg,
    borderRadius: BORDER_RADIUS.md,
    ...SHADOWS.button,
  },
  ctaBtnText: { color: COLORS.white, fontSize: FONT_SIZES.md, fontWeight: '600' },
  grid: {
    flexDirection: 'row', flexWrap: 'wrap',
    gap: SPACING.md,
  },
  card: {
    flexBasis: '47%',
    backgroundColor: COLORS.surface,
    borderRadius: BORDER_RADIUS.lg,
    padding: SPACING.lg,
    borderWidth: 1, borderColor: COLORS.border,
    ...SHADOWS.card,
  },
  cardIconWrap: {
    width: 44, height: 44, borderRadius: BORDER_RADIUS.md,
    backgroundColor: COLORS.primarySoft,
    alignItems: 'center', justifyContent: 'center',
    marginBottom: SPACING.md,
  },
  cardTitle: {
    fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.ink,
    marginBottom: 4,
  },
  cardSub: { fontSize: FONT_SIZES.xs, color: COLORS.ink3, lineHeight: 16 },
  cardPrice: {
    fontSize: FONT_SIZES.xs, color: COLORS.primary,
    marginTop: SPACING.sm, fontWeight: '600',
  },
});

export default CatalogScreen;
