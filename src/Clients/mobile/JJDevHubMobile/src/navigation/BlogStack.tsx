import React from 'react';
import {createNativeStackNavigator} from '@react-navigation/native-stack';
import {BlogListScreen} from '../screens/BlogListScreen';
import {BlogPostScreen} from '../screens/BlogPostScreen';
import type {BlogPost} from '../data/blogMock';

export type BlogStackParamList = {
  BlogList: undefined;
  BlogPost: {post: BlogPost};
};

const Stack = createNativeStackNavigator<BlogStackParamList>();

export function BlogStackNavigator() {
  return (
    <Stack.Navigator>
      <Stack.Screen
        name="BlogList"
        component={BlogListScreen}
        options={{title: 'Blog'}}
      />
      <Stack.Screen
        name="BlogPost"
        component={BlogPostScreen}
        options={({route}) => ({title: route.params.post.title})}
      />
    </Stack.Navigator>
  );
}
