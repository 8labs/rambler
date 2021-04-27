
 
 
 



declare namespace System {
    type Guid = string;
}


declare namespace Rambler {
	interface AnopeChannelModerator {
		channel: string;
		last_seen: string;
		level: Rambler.ChannelModeratorLevels;
		nick: string;
	}
	interface AnopeChannelRegistration {
		forbidden: boolean;
		forbidreason: string;
		founder: string;
		last_topic: string;
		last_used: string;
		name: string;
		successor: string;
		time_registered: string;
	}
	interface AnopeImport {
		Channels: Rambler.AnopeChannelRegistration[];
		Moderators: Rambler.AnopeChannelModerator[];
		Nicknames: Rambler.AnopeNicknameRegistration[];
	}
	interface AnopeNicknameRegistration {
		email: string;
		last_connection_date: string;
		nick: string;
		password: string;
		register_date: string;
	}
	interface Auth {
		Password: string;
		Username: string;
	}
	interface AuthRequest {
		Token: string;
	}
	interface AuthResponse {
		ConnectionCount: number;
		Nick: string;
		SocketId: System.Guid;
		UserId: System.Guid;
	}
	interface BotToken {
		Description: string;
		Expres: Date;
		Id: System.Guid;
		Name: string;
		Permissions: string[];
	}
	interface ChannelBanDto {
		ChannelId: System.Guid;
		ChannelName: string;
		Created: Date;
		CreatedBy: string;
		Expires: Date;
		Id: number;
		Level: Rambler.Contracts.Api.BanLevel;
		Nick: string;
		Reason: string;
		UserId: System.Guid;
	}
	interface ChannelBannedResponse {
		ChannelName: string;
		Expires: Date;
		Level: Rambler.Contracts.Api.BanLevel;
		ModeratorNick: string;
		Reason: string;
		UserId: System.Guid;
	}
	interface ChannelBanRequest {
		ChannelId: System.Guid;
		ExpiresMinutes: number;
		Reason: string;
		UserId: System.Guid;
	}
	interface ChannelDto {
		AllowGuests: boolean;
		Created: Date;
		Description: string;
		Id: System.Guid;
		IsSecret: boolean;
		LastActivity: Date;
		LastModified: Date;
		MaxUsers: number;
		Name: string;
		OwnerId: System.Guid;
		OwnerNick: string;
		UserCount: number;
	}
	interface ChannelJoinedResponse {
		IsGuest: boolean;
		Level: Rambler.Contracts.Api.ModerationLevel;
		Nick: string;
		UserId: System.Guid;
	}
	interface ChannelMessageRequest {
		ChannelId: System.Guid;
		Message: string;
	}
	interface ChannelMessageResponse {
		Message: string;
		Nick: string;
		Type: string;
		UserId: System.Guid;
	}
	interface ChannelModeratorDto {
		Created: Date;
		Id: number;
		Level: Rambler.Contracts.Api.ModerationLevel;
		Nick: string;
		UserId: System.Guid;
	}
	interface ChannelPartRequest {
		ChannelId: System.Guid;
	}
	interface ChannelPartResponse {
		UserId: System.Guid;
	}
	interface ChannelUpdateResponse {
		AllowsGuests: boolean;
		Description: string;
		IsSecret: boolean;
		LastModified: Date;
		MaxUsers: number;
		Name: string;
	}
	interface ChannelUsersRequest {
		ChannelId: System.Guid;
	}
	interface ChannelUsersResponse {
		ChannelId: System.Guid;
		Users: Rambler.RoomUser[];
	}
	interface ChannelUserUpdateResponse {
		IsGuest: boolean;
		Level: Rambler.Contracts.Api.ModerationLevel;
		Nick: string;
		UserId: System.Guid;
	}
	interface ChannelWarnedResponse {
		Warnings: Rambler.Warning[];
	}
	interface DirectMessageRequest {
		Message: string;
		UserId: System.Guid;
	}
	interface DirectMessageResponse {
		EchoUser: System.Guid;
		Message: string;
		Nick: string;
		Type: string;
		UserId: System.Guid;
	}
	interface DisconnectRequest {
	}
	interface ErrorResponse {
		Code: number;
	}
	interface GetDmMessages {
		Last: number;
		Token: string;
		UserId: System.Guid;
	}
	interface GetSubscriptionMessages {
		Id: System.Guid;
		Last: number;
		Token: string;
	}
	interface IdentityToken {
		Expires: number;
		IsGuest: boolean;
		IsValidated: boolean;
		Level: number;
		Nick: string;
		UserId: System.Guid;
	}
	interface IgnoreDto {
		Id: number;
		IgnoredOn: Date;
		IgnoreId: System.Guid;
		IgnoreNick: string;
		UserId: System.Guid;
	}
	interface IResponse {
		Id: number;
		Subscription: System.Guid;
		Timestamp: number;
		Type: string;
	}
	interface IResponseDistributor {
	}
	interface IResponseProcesor<T> {
	}
	interface IResponsePublisher {
	}
	interface JoinRequest {
		ChannelId: System.Guid;
		ChannelName: string;
	}
	interface JoinResponse {
		AllowsGuests: boolean;
		ChannelId: System.Guid;
		Description: string;
		IsSecret: boolean;
		Level: Rambler.Contracts.Api.ModerationLevel;
		MaxUsers: number;
		Name: string;
		UserId: System.Guid;
	}
	interface ListChannel {
		AllowsGuests: boolean;
		Description: string;
		Id: System.Guid;
		IsSecret: boolean;
		MaxUsers: number;
		Name: string;
		UserCount: number;
	}
	interface ListResponse {
		Channels: Rambler.ListChannel[];
	}
	interface MessageTypes {
	}
	interface PasswordReset {
		Captcha: string;
		Email: string;
		NewPassword: string;
		Token: string;
	}
	interface Register {
		Captcha: string;
		Email: string;
		Nick: string;
		Password: string;
		PasswordVerify: string;
	}
	interface Response<T> {
		Data: T;
		Id: number;
		Subscription: System.Guid;
		Timestamp: number;
		Type: string;
	}
	interface RoomUser {
		Id: System.Guid;
		IsGuest: boolean;
		ModLevel: number;
		Nick: string;
	}
	interface ServerBanDto {
		BannedNick: string;
		BannedUserId: System.Guid;
		Created: Date;
		CreatedById: System.Guid;
		CreatedByNick: string;
		Expires: Date;
		Id: number;
		IPFilter: string;
		Reason: string;
	}
	interface ServerBannedResponse {
		Expires: Date;
		Reason: string;
	}
	interface ServerUserInfoDto {
		Channels: Rambler.UserChannelInfo[];
		ConnectionCount: number;
		Info: Rambler.UserInfo;
		IPAddresses: string[];
		RelatedUsers: Rambler.UserInfo[];
	}
	interface UserChannelInfo {
		ChannelId: System.Guid;
		Level: Rambler.Contracts.Api.ModerationLevel;
		Name: string;
	}
	interface UserInfo {
		Nick: string;
		UserId: System.Guid;
	}
	interface UserOfflineResponse {
		UserId: System.Guid;
	}
	interface UserResetDto {
		NewPassword: string;
		UserId: System.Guid;
		VerifyNewPassword: string;
	}
	interface UserSettingsDto {
		Ignores: Rambler.IgnoreDto[];
	}
	interface Warning {
		Expires: Date;
		Reason: string;
		UserId: System.Guid;
	}
}
