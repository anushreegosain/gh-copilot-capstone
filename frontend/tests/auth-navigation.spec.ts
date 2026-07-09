import { test, expect } from '@playwright/test';

test('navigates from sign in page to sign up and back', async ({ page }) => {
  await page.goto('/');

  await page.locator('header').getByRole('button', { name: 'Sign In' }).click();
  await expect(page).toHaveURL(/\/signin$/);
  await expect(page.getByRole('heading', { name: 'Sign In' })).toBeVisible();

  await page.getByRole('main').getByRole('button', { name: 'Sign Up' }).click();
  await expect(page).toHaveURL(/\/signup$/);
  await expect(page.getByRole('heading', { name: 'Create Account' })).toBeVisible();

  await page.getByRole('main').getByRole('button', { name: 'Sign In' }).click();
  await expect(page).toHaveURL(/\/signin$/);
  await expect(page.getByRole('heading', { name: 'Sign In' })).toBeVisible();
});
