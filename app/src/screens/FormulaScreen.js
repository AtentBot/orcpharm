import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  Alert,
  ActivityIndicator,
  TextInput,
  FlatList,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { Feather } from '@expo/vector-icons';
import * as api from '../services/api';
import { formatCurrency } from '../utils/formatters';
import { COLORS, GRADIENTS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';

const FormulaScreen = ({ navigation }) => {
  const [productTypes, setProductTypes] = useState([]);
  const [selectedType, setSelectedType] = useState(null);
  const [ingredients, setIngredients] = useState([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState([]);
  const [searching, setSearching] = useState(false);
  const [calculating, setCalculating] = useState(false);
  const [totalPrice, setTotalPrice] = useState(0);
  const [loading, setLoading] = useState(true);

  useEffect(() => { loadProductTypes(); }, []);

  useEffect(() => {
    if (selectedType && ingredients.length > 0) {
      calculatePrice();
    } else {
      setTotalPrice(0);
    }
  }, [selectedType, ingredients]);

  const loadProductTypes = async () => {
    try {
      const result = await api.getProductTypes();
      if (result.success && result.productTypes) {
        setProductTypes(result.productTypes);
        if (result.productTypes.length > 0) setSelectedType(result.productTypes[0]);
      }
    } catch (error) {
      console.error('Erro ao carregar tipos:', error);
    } finally {
      setLoading(false);
    }
  };

  const searchIngredients = async (query) => {
    if (!query || query.length < 2) { setSearchResults([]); return; }
    setSearching(true);
    try {
      const result = await api.autocompleteIngredients(query);
      if (result.success && result.ingredients) setSearchResults(result.ingredients);
    } catch (error) {
      console.error('Erro na busca:', error);
    } finally {
      setSearching(false);
    }
  };

  const addIngredient = (ingredient) => {
    if (ingredients.some(i => i.id === ingredient.id)) {
      Alert.alert('Atenção', 'Este ingrediente já foi adicionado.');
      return;
    }
    setIngredients([...ingredients, { ...ingredient, quantity: 1, unit: ingredient.defaultUnit || 'mg' }]);
    setSearchQuery('');
    setSearchResults([]);
  };

  const updateIngredientQuantity = (id, quantity) => {
    setIngredients(ingredients.map(i => i.id === id ? { ...i, quantity: parseFloat(quantity) || 0 } : i));
  };

  const updateIngredientUnit = (id, unit) => {
    setIngredients(ingredients.map(i => i.id === id ? { ...i, unit } : i));
  };

  const removeIngredient = (id) => {
    setIngredients(ingredients.filter(i => i.id !== id));
  };

  const calculatePrice = async () => {
    if (!selectedType || ingredients.length === 0) return;
    setCalculating(true);
    try {
      const result = await api.calculateFormula(
        selectedType.id,
        ingredients.map(i => ({ ingredientId: i.id, quantity: i.quantity, unit: i.unit }))
      );
      if (result.success) setTotalPrice(result.totalPrice || 0);
    } catch (error) {
      console.error('Erro ao calcular:', error);
    } finally {
      setCalculating(false);
    }
  };

  const addToCart = async () => {
    if (!selectedType || ingredients.length === 0) {
      Alert.alert('Atenção', 'Adicione ao menos um ingrediente.');
      return;
    }
    try {
      const result = await api.addFormulaToCart({
        productTypeId: selectedType.id,
        ingredients: ingredients.map(i => ({
          ingredientId: i.id, ingredientName: i.name, quantity: i.quantity, unit: i.unit,
        })),
      });
      if (result.success) {
        Alert.alert('Adicionado ao carrinho!', 'Sua fórmula foi adicionada com sucesso.',
          [{ text: 'Ver carrinho', onPress: () => navigation.navigate('Cart') }, { text: 'Continuar', style: 'cancel' }]
        );
      } else {
        Alert.alert('Erro', result.message || 'Não foi possível adicionar ao carrinho.');
      }
    } catch (error) {
      Alert.alert('Erro', 'Ocorreu um erro. Tente novamente.');
    }
  };

  if (loading) {
    return (
      <LinearGradient colors={GRADIENTS.background} style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={COLORS.primary} />
      </LinearGradient>
    );
  }

  return (
    <LinearGradient colors={GRADIENTS.background} style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton} onPress={() => navigation.goBack()}>
          <View style={styles.backButtonCircle}>
            <Feather name="arrow-left" size={20} color={COLORS.primary} />
          </View>
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Fórmula Personalizada</Text>
        <View style={{ width: 44 }} />
      </View>

      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
        keyboardShouldPersistTaps="handled"
      >
        {/* Product type */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Tipo de produto</Text>
          <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.typesContainer}>
            {productTypes.map((type) => {
              const isSelected = selectedType?.id === type.id;
              return (
                <TouchableOpacity
                  key={type.id}
                  style={[styles.typeCard, isSelected && styles.typeCardSelected]}
                  onPress={() => setSelectedType(type)}
                  activeOpacity={0.7}
                >
                  <Text style={styles.typeEmoji}>{type.emoji || '💊'}</Text>
                  <Text style={[styles.typeName, isSelected && styles.typeNameSelected]}>{type.name}</Text>
                </TouchableOpacity>
              );
            })}
          </ScrollView>
        </View>

        {/* Ingredient search */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Ingredientes</Text>
          <View style={styles.searchContainer}>
            <View style={styles.searchIconWrapper}>
              <Feather name="search" size={18} color={COLORS.textMuted} />
            </View>
            <TextInput
              style={styles.searchInput}
              placeholder="Buscar ingrediente..."
              placeholderTextColor={COLORS.textMuted}
              value={searchQuery}
              onChangeText={(text) => { setSearchQuery(text); searchIngredients(text); }}
            />
            {searching && <ActivityIndicator size="small" color={COLORS.primary} />}
          </View>

          {searchResults.length > 0 && (
            <View style={styles.searchResults}>
              {searchResults.map((item) => (
                <TouchableOpacity key={item.id} style={styles.searchResultItem} onPress={() => addIngredient(item)}>
                  <Text style={styles.searchResultText}>{item.name}</Text>
                  <View style={styles.addIconCircle}>
                    <Feather name="plus" size={16} color={COLORS.white} />
                  </View>
                </TouchableOpacity>
              ))}
            </View>
          )}
        </View>

        {/* Added ingredients */}
        {ingredients.length > 0 && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Sua fórmula ({ingredients.length} itens)</Text>
            {ingredients.map((ingredient) => (
              <View key={ingredient.id} style={styles.ingredientCard}>
                <View style={styles.ingredientAccent} />
                <View style={styles.ingredientBody}>
                  <View style={styles.ingredientInfo}>
                    <Text style={styles.ingredientName}>{ingredient.name}</Text>
                    <View style={styles.ingredientControls}>
                      <TextInput
                        style={styles.quantityInput}
                        keyboardType="numeric"
                        value={String(ingredient.quantity)}
                        onChangeText={(text) => updateIngredientQuantity(ingredient.id, text)}
                      />
                      <View style={styles.unitSelector}>
                        {['mg', 'g', 'ml'].map((unit) => (
                          <TouchableOpacity
                            key={unit}
                            style={[styles.unitButton, ingredient.unit === unit && styles.unitButtonActive]}
                            onPress={() => updateIngredientUnit(ingredient.id, unit)}
                          >
                            <Text style={[styles.unitButtonText, ingredient.unit === unit && styles.unitButtonTextActive]}>{unit}</Text>
                          </TouchableOpacity>
                        ))}
                      </View>
                    </View>
                  </View>
                  <TouchableOpacity style={styles.removeButton} onPress={() => removeIngredient(ingredient.id)}>
                    <Feather name="trash-2" size={18} color={COLORS.error} />
                  </TouchableOpacity>
                </View>
              </View>
            ))}
          </View>
        )}

        {/* Summary */}
        {ingredients.length > 0 && (
          <View style={styles.summaryCard}>
            <View style={styles.summaryRow}>
              <Text style={styles.summaryLabel}>Tipo:</Text>
              <Text style={styles.summaryValue}>{selectedType?.name}</Text>
            </View>
            <View style={styles.summaryRow}>
              <Text style={styles.summaryLabel}>Ingredientes:</Text>
              <Text style={styles.summaryValue}>{ingredients.length}</Text>
            </View>
            <View style={styles.divider} />
            <View style={styles.summaryRow}>
              <Text style={styles.totalLabel}>Total estimado:</Text>
              {calculating ? (
                <ActivityIndicator size="small" color={COLORS.primary} />
              ) : (
                <Text style={styles.totalValue}>{formatCurrency(totalPrice)}</Text>
              )}
            </View>
          </View>
        )}

        {/* Add to cart */}
        {ingredients.length > 0 && (
          <TouchableOpacity onPress={addToCart} activeOpacity={0.8}>
            <LinearGradient colors={GRADIENTS.primary} style={styles.addButton}>
              <Feather name="shopping-cart" size={20} color={COLORS.white} />
              <Text style={styles.addButtonText}>Adicionar ao carrinho</Text>
            </LinearGradient>
          </TouchableOpacity>
        )}

        <View style={{ height: 100 }} />
      </ScrollView>
    </LinearGradient>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1 },
  loadingContainer: { flex: 1, alignItems: 'center', justifyContent: 'center' },

  header: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingTop: SPACING.xxxl + 10, paddingHorizontal: SPACING.lg, paddingBottom: SPACING.md,
  },
  backButton: { padding: 2 },
  backButtonCircle: {
    width: 44, height: 44, borderRadius: 22,
    backgroundColor: COLORS.primaryMuted, alignItems: 'center', justifyContent: 'center',
  },
  headerTitle: { fontSize: FONT_SIZES.lg, fontWeight: '700', color: COLORS.text },
  scrollView: { flex: 1 },
  scrollContent: { padding: SPACING.lg },

  section: { marginBottom: SPACING.xl },
  sectionTitle: { fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.text, marginBottom: SPACING.md },

  typesContainer: { gap: SPACING.sm },
  typeCard: {
    backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.xxl, padding: SPACING.md,
    alignItems: 'center', minWidth: 88, marginRight: SPACING.sm,
    borderWidth: 2, borderColor: 'transparent', ...SHADOWS.small,
  },
  typeCardSelected: { borderColor: COLORS.primary, backgroundColor: COLORS.primaryMuted },
  typeEmoji: { fontSize: 26, marginBottom: SPACING.xs },
  typeName: { fontSize: FONT_SIZES.xs, color: COLORS.textSecondary, fontWeight: '500' },
  typeNameSelected: { color: COLORS.primary, fontWeight: '700' },

  searchContainer: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: COLORS.white,
    borderRadius: BORDER_RADIUS.xxl, paddingVertical: 14, paddingHorizontal: SPACING.md,
    gap: SPACING.sm, borderWidth: 2, borderColor: COLORS.border,
  },
  searchIconWrapper: {
    width: 32, height: 32, borderRadius: 16,
    backgroundColor: COLORS.borderLight, alignItems: 'center', justifyContent: 'center',
  },
  searchInput: { flex: 1, fontSize: FONT_SIZES.md, color: COLORS.text },

  searchResults: {
    backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.lg, marginTop: SPACING.sm,
    borderWidth: 1, borderColor: COLORS.borderLight, maxHeight: 220, ...SHADOWS.medium, overflow: 'hidden',
  },
  searchResultItem: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingVertical: 14, paddingHorizontal: SPACING.md, borderBottomWidth: 1, borderBottomColor: COLORS.borderLight,
  },
  searchResultText: { flex: 1, fontSize: FONT_SIZES.md, color: COLORS.text },
  addIconCircle: {
    width: 28, height: 28, borderRadius: 14,
    backgroundColor: COLORS.primary, alignItems: 'center', justifyContent: 'center',
  },

  ingredientCard: {
    flexDirection: 'row', backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.lg,
    marginBottom: SPACING.sm, overflow: 'hidden', ...SHADOWS.small,
  },
  ingredientAccent: { width: 4, backgroundColor: COLORS.primary },
  ingredientBody: { flex: 1, flexDirection: 'row', alignItems: 'center', padding: SPACING.md },
  ingredientInfo: { flex: 1 },
  ingredientName: { fontSize: FONT_SIZES.md, fontWeight: '600', color: COLORS.text, marginBottom: SPACING.sm },
  ingredientControls: { flexDirection: 'row', alignItems: 'center', gap: SPACING.md },
  quantityInput: {
    width: 64, backgroundColor: COLORS.backgroundLight, borderRadius: BORDER_RADIUS.md,
    padding: SPACING.sm, fontSize: FONT_SIZES.md, textAlign: 'center', color: COLORS.text, fontWeight: '600',
  },

  unitSelector: { flexDirection: 'row', gap: 4 },
  unitButton: { paddingHorizontal: 12, paddingVertical: 6, borderRadius: 20, backgroundColor: COLORS.backgroundLight },
  unitButtonActive: { backgroundColor: COLORS.primary },
  unitButtonText: { fontSize: FONT_SIZES.xs, color: COLORS.textMuted, fontWeight: '600' },
  unitButtonTextActive: { color: COLORS.white },
  removeButton: { padding: SPACING.sm },

  summaryCard: {
    backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.xxl, padding: SPACING.lg + 4,
    marginBottom: SPACING.lg, borderWidth: 1, borderColor: COLORS.borderLight, ...SHADOWS.medium,
  },
  summaryRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: SPACING.sm },
  summaryLabel: { fontSize: FONT_SIZES.md, color: COLORS.textSecondary },
  summaryValue: { fontSize: FONT_SIZES.md, color: COLORS.text, fontWeight: '600' },
  divider: { height: 1, backgroundColor: COLORS.borderLight, marginVertical: SPACING.md },
  totalLabel: { fontSize: FONT_SIZES.lg, fontWeight: '700', color: COLORS.text },
  totalValue: { fontSize: FONT_SIZES.xl, fontWeight: '800', color: COLORS.primary },

  addButton: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
    paddingVertical: 18, paddingHorizontal: SPACING.xl, borderRadius: BORDER_RADIUS.xxl, gap: SPACING.sm,
    ...SHADOWS.colored(COLORS.primary),
  },
  addButtonText: { fontSize: FONT_SIZES.lg, fontWeight: '700', color: COLORS.white },
});

export default FormulaScreen;
