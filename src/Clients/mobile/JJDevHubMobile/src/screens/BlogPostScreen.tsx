import React from 'react';
import {ScrollView, StyleSheet, View} from 'react-native';
import {Text, useTheme} from 'react-native-paper';
import type {NativeStackScreenProps} from '@react-navigation/native-stack';
import type {BlogStackParamList} from '../navigation/BlogStack';

type Props = NativeStackScreenProps<BlogStackParamList, 'BlogPost'>;

export function BlogPostScreen({route}: Props) {
  const theme = useTheme();
  const {post} = route.params;

  return (
    <ScrollView contentContainerStyle={styles.pad}>
      <Text variant="headlineSmall">{post.title}</Text>
      <Text variant="bodyMedium" style={styles.intro}>
        {post.intro}
      </Text>
      <View
        style={[
          styles.codeBlock,
          {backgroundColor: theme.colors.surfaceVariant},
        ]}>
        <Text
          selectable
          style={[styles.code, {color: theme.colors.onSurfaceVariant}]}>
          {post.codeSample}
        </Text>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  pad: {padding: 16, paddingBottom: 32},
  intro: {marginTop: 12},
  codeBlock: {
    marginTop: 16,
    padding: 12,
    borderRadius: 8,
  },
  code: {
    fontFamily: 'monospace',
    fontSize: 12,
  },
});
