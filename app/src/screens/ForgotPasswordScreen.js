import React, { useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, ScrollView, Alert, ActivityIndicator,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { Feather } from '@expo/vector-icons';
import * as api from '../services/api';
import { formatPhone } from '../utils/formatters';
import { COLORS, GRADIENTS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';

const ForgotPasswordScreen = ({ navigation }) => {
  const [phone, setPhone] = useState('');
  const [loading, setLoading] = useState(false);

  const handleRequest = async () => {
    const phoneDigits = phone.replace(/\D/g, '');
    if (phoneDigits.length < 10) {
      Alert.alert('WhatsApp inválido', 'Digite o número com DDD.');
      return;
    }
    setLoading(true);
    try {
      const result = await api.requestPasswordReset(phoneDigits);
      if (result.success) {
        Alert.alert(
          'Código enviado',
          'Verifique seu WhatsApp e digite o código na próxima tela.',
          [{ text: 'OK', onPress: () => navigation.navigate('ResetPassword', { phone: phoneDigits }) }]
        );
      } else {
        Alert.alert('Erro', result.message || 'Não foi possível enviar o código.');
      }
    } catch (error) {
      Alert.alert('Erro', 'Não foi possível conectar.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <LinearGradient colors={GRADIENTS.splash} style={styles.headerGradient}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
          <Feather name="arrow-left" size={24} color={COLORS.white} />
        </TouchableOpacity>
        <View style={styles.logoContainer}>
          <View style={styles.logoCircle}>
            <Feather name="lock" size={32} color={COLORS.white} />
          </View>
          <Text style={styles.logoText}>Esqueceu a senha?</Text>
          <Text style={styles.subtitle}>Vamos enviar um código pelo WhatsApp</Text>
        </View>
      </LinearGradient>

      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        style={styles.formWrapper}
      >
        <ScrollView contentContainerStyle={styles.scrollContent} keyboardShouldPersistTaps="handled">
          <View style={styles.formCard}>
            <Text style={styles.instructionText}>
              Digite o WhatsApp cadastrado e enviaremos um código de 6 dígitos.
            </Text>

            <View style={styles.inputContainer}>
              <View style={styles.inputAccent} />
              <Feather name="phone" size={20} color={COLORS.textMuted} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="WhatsApp (com DDD)"
                placeholderTextColor={COLORS.textMuted}
                value={phone}
                onChangeText={(t) => setPhone(formatPhone(t))}
                keyboardType="phone-pad"
                maxLength={15}
                editable={!loading}
              />
            </View>

            <TouchableOpacity onPress={handleRequest} disabled={loading} activeOpacity={0.8}>
              <View style={styles.buttonShadow}>
                <LinearGradient
                  colors={GRADIENTS.primary}
                  start={{ x: 0, y: 0 }}
                  end={{ x: 1, y: 0 }}
                  style={styles.button}
                >
                  {loading ? (
                    <ActivityIndicator color={COLORS.white} />
                  ) : (
                    <>
                      <Text style={styles.buttonText}>Enviar código</Text>
                      <Feather name="send" size={20} color={COLORS.white} />
                    </>
                  )}
                </LinearGradient>
              </View>
            </TouchableOpacity>

            <TouchableOpacity onPress={() => navigation.goBack()} style={styles.linkButton}>
              <Text style={styles.linkText}>Voltar para login</Text>
            </TouchableOpacity>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: COLORS.background },
  headerGradient: { paddingTop: 60, paddingBottom: 80, paddingHorizontal: SPACING.lg, position: 'relative' },
  backButton: { position: 'absolute', top: 50, left: SPACING.lg, padding: SPACING.sm, zIndex: 10 },
  logoContainer: { alignItems: 'center', marginTop: SPACING.lg },
  logoCircle: {
    width: 72, height: 72, borderRadius: 36,
    backgroundColor: 'rgba(255,255,255,0.18)',
    alignItems: 'center', justifyContent: 'center', marginBottom: SPACING.md,
  },
  logoText: { fontSize: FONT_SIZES.xl, fontWeight: '700', color: COLORS.white, marginBottom: 6 },
  subtitle: { fontSize: FONT_SIZES.sm, color: 'rgba(255,255,255,0.85)' },
  formWrapper: { flex: 1, marginTop: -40 },
  scrollContent: { paddingHorizontal: SPACING.lg },
  formCard: {
    backgroundColor: COLORS.white,
    borderRadius: BORDER_RADIUS.lg,
    padding: SPACING.xl,
    ...SHADOWS.card,
  },
  instructionText: {
    fontSize: FONT_SIZES.md, color: COLORS.textSecondary,
    marginBottom: SPACING.lg, textAlign: 'center',
  },
  inputContainer: {
    flexDirection: 'row', alignItems: 'center',
    backgroundColor: COLORS.backgroundAlt,
    borderRadius: BORDER_RADIUS.md, paddingHorizontal: SPACING.md,
    height: 56, marginBottom: SPACING.md, position: 'relative', overflow: 'hidden',
  },
  inputAccent: {
    position: 'absolute', left: 0, top: 0, bottom: 0, width: 4, backgroundColor: COLORS.primary,
  },
  inputIcon: { marginLeft: SPACING.sm, marginRight: SPACING.sm },
  input: { flex: 1, fontSize: FONT_SIZES.md, color: COLORS.text },
  buttonShadow: { ...SHADOWS.button, marginTop: SPACING.md },
  button: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
    gap: SPACING.sm, height: 56, borderRadius: BORDER_RADIUS.md,
  },
  buttonText: { fontSize: FONT_SIZES.md, fontWeight: '600', color: COLORS.white },
  linkButton: { marginTop: SPACING.lg, alignItems: 'center' },
  linkText: { color: COLORS.primary, fontSize: FONT_SIZES.sm, fontWeight: '500' },
});

export default ForgotPasswordScreen;
