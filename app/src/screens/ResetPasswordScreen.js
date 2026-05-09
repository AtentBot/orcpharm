import React, { useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, ScrollView, Alert, ActivityIndicator,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { Feather } from '@expo/vector-icons';
import * as api from '../services/api';
import { COLORS, GRADIENTS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';

const ResetPasswordScreen = ({ route, navigation }) => {
  const phone = route?.params?.phone || '';
  const [code, setCode] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleReset = async () => {
    if (!code || !newPassword || !confirmPassword) {
      Alert.alert('Atenção', 'Preencha todos os campos.');
      return;
    }
    if (code.length !== 6) {
      Alert.alert('Código inválido', 'O código deve ter 6 dígitos.');
      return;
    }
    if (newPassword.length < 8) {
      Alert.alert('Senha fraca', 'A senha deve ter no mínimo 8 caracteres.');
      return;
    }
    if (newPassword !== confirmPassword) {
      Alert.alert('Senhas diferentes', 'A confirmação não confere.');
      return;
    }

    setLoading(true);
    try {
      const result = await api.resetPassword(phone, code, newPassword, confirmPassword);
      if (result.success) {
        Alert.alert(
          'Senha redefinida',
          'Você já pode entrar com sua nova senha.',
          [{ text: 'OK', onPress: () => navigation.navigate('Login') }]
        );
      } else {
        Alert.alert('Erro', result.message || 'Não foi possível redefinir.');
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
            <Feather name="shield" size={32} color={COLORS.white} />
          </View>
          <Text style={styles.logoText}>Nova senha</Text>
          <Text style={styles.subtitle}>Digite o código recebido</Text>
        </View>
      </LinearGradient>

      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        style={styles.formWrapper}
      >
        <ScrollView contentContainerStyle={styles.scrollContent} keyboardShouldPersistTaps="handled">
          <View style={styles.formCard}>
            <Text style={styles.instructionText}>
              Enviamos um código de 6 dígitos para o WhatsApp. Digite-o abaixo junto com sua nova senha.
            </Text>

            <View style={styles.inputContainer}>
              <View style={styles.inputAccent} />
              <Feather name="hash" size={20} color={COLORS.textMuted} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="Código de 6 dígitos"
                placeholderTextColor={COLORS.textMuted}
                value={code}
                onChangeText={(t) => setCode(t.replace(/\D/g, '').slice(0, 6))}
                keyboardType="number-pad"
                maxLength={6}
                editable={!loading}
              />
            </View>

            <View style={styles.inputContainer}>
              <View style={styles.inputAccent} />
              <Feather name="lock" size={20} color={COLORS.textMuted} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="Nova senha (mín. 8 caracteres)"
                placeholderTextColor={COLORS.textMuted}
                value={newPassword}
                onChangeText={setNewPassword}
                secureTextEntry={!showPassword}
                editable={!loading}
              />
              <TouchableOpacity onPress={() => setShowPassword(!showPassword)} style={styles.eyeButton}>
                <Feather name={showPassword ? 'eye-off' : 'eye'} size={20} color={COLORS.textMuted} />
              </TouchableOpacity>
            </View>

            <View style={styles.inputContainer}>
              <View style={styles.inputAccent} />
              <Feather name="lock" size={20} color={COLORS.textMuted} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="Confirmar nova senha"
                placeholderTextColor={COLORS.textMuted}
                value={confirmPassword}
                onChangeText={setConfirmPassword}
                secureTextEntry={!showPassword}
                editable={!loading}
              />
            </View>

            <TouchableOpacity onPress={handleReset} disabled={loading} activeOpacity={0.8}>
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
                      <Text style={styles.buttonText}>Redefinir senha</Text>
                      <Feather name="check" size={20} color={COLORS.white} />
                    </>
                  )}
                </LinearGradient>
              </View>
            </TouchableOpacity>

            <TouchableOpacity onPress={() => navigation.navigate('ForgotPassword')} style={styles.linkButton}>
              <Text style={styles.linkText}>Não recebi o código - reenviar</Text>
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
  scrollContent: { paddingHorizontal: SPACING.lg, paddingBottom: SPACING.xl },
  formCard: {
    backgroundColor: COLORS.white,
    borderRadius: BORDER_RADIUS.lg,
    padding: SPACING.xl,
    ...SHADOWS.card,
  },
  instructionText: {
    fontSize: FONT_SIZES.sm, color: COLORS.textSecondary,
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
  eyeButton: { padding: SPACING.sm },
  buttonShadow: { ...SHADOWS.button, marginTop: SPACING.md },
  button: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
    gap: SPACING.sm, height: 56, borderRadius: BORDER_RADIUS.md,
  },
  buttonText: { fontSize: FONT_SIZES.md, fontWeight: '600', color: COLORS.white },
  linkButton: { marginTop: SPACING.lg, alignItems: 'center' },
  linkText: { color: COLORS.primary, fontSize: FONT_SIZES.sm, fontWeight: '500' },
});

export default ResetPasswordScreen;
