/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License 
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty  
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not, 
 * see <https://www.gnu.org/licenses/>.
 */

const PasswordPolicy = {
    minLength: 12,
    maxLength: 128
};

const PasswordStrength = {
    rules: [
        {
            test: (p) => p.length >= PasswordPolicy.minLength,
            message: `At least ${PasswordPolicy.minLength} characters`
        },
        {
            test: (p) => p.length <= PasswordPolicy.maxLength,
            message: `No more than ${PasswordPolicy.maxLength} characters`
        },
        {
            test: (p) => /[A-Z]/.test(p),
            message: "At least one uppercase letter"
        },
        {
            test: (p) => /[a-z]/.test(p),
            message: "At least one lowercase letter"
        },
        {
            test: (p) => /[0-9]/.test(p),
            message: "At least one number"
        },
        {
            test: (p) => /[^A-Za-z0-9]/.test(p),
            message: "At least one special character"
        },
        {
            test: (p) => !/(.)\1{2,}/.test(p),
            message: "No character repeated more than twice consecutively"
        },
        {
            test: (p) => !/^(password|123456|qwerty)/i.test(p),
            message: "Cannot start with common patterns"
        }
    ],

    validate(password) {
        const failures = this.rules
            .filter(rule => !rule.test(password))
            .map(rule => rule.message);

        return {
            valid: failures.length === 0,
            failures
        };
    },

    score(password) {
        const passed = this.rules.filter(rule => rule.test(password)).length;
        return Math.round((passed / this.rules.length) * 100);
    }
};