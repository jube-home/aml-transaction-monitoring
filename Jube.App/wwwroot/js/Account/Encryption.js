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

async function encryptPassword(password) {
    if (!passwordAsymmetricEncryption) return password;

    const pem = atob(passwordAsymmetricEncryptionPublicKey);

    const pemBody = pem
        .replace(/-----BEGIN PUBLIC KEY-----/, '')
        .replace(/-----END PUBLIC KEY-----/, '')
        .replace(/\s/g, '');

    const binaryDer = Uint8Array.from(atob(pemBody), c => c.charCodeAt(0));

    const publicKey = await crypto.subtle.importKey(
        "spki",
        binaryDer,
        {name: "RSA-OAEP", hash: "SHA-256"},
        false,
        ["encrypt"]
    );

    const encoded = new TextEncoder().encode(password);
    const encrypted = await crypto.subtle.encrypt(
        {name: "RSA-OAEP"},
        publicKey,
        encoded
    );

    return btoa(String.fromCharCode(...new Uint8Array(encrypted)));
}